using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using EAVFramework.Configuration;
using EAVFW.Extensions.Manifest.SDK;
using EAVFW.Extensions.Manifest.SDK.Migrations;
using EAVFW.Extensions.SecurityModel;
using Grpc.Core;
using IdentityModel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Resource = Aspire.Hosting.ApplicationModel.Resource;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    public record DatabaseCreatedAnnotation() : IResourceAnnotation
    {
        public int Attempt { get; set; } = 1;
        public bool Success { get; set; }
    }

    public record EAVFWMigrationdAnnotation() : IResourceAnnotation
    {
        public bool Success { get; set; }
        public int Attempt { get; set; } = 1;
    }

    public record EAVFWBuildAnnotation() : IResourceAnnotation
    {
        public ProjectResource ProjectResource { get; set; }
    }

    public class EavBuildResource : Resource
    {
        public EavBuildResource(string name, string command, string workingdirectory, params string[] arguments) : base(name)
        {
            Command = command;
            Workingdirectory = workingdirectory;
            Arguments = arguments;
        }

        public string Command { get; }
        public string Workingdirectory { get; }
        public string[] Arguments { get; }
        public ProjectResource Project { get;  set; }
    }
    internal sealed class ProcessResult
    {
        public ProcessResult(int exitCode)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }
    internal sealed class ProcessSpec
    {
        public string ExecutablePath { get; }
        public string? WorkingDirectory { get; init; }
        public IDictionary<string, string> EnvironmentVariables { get; init; } = new Dictionary<string, string>();
        public bool InheritEnv { get; init; } = true;
        public string? Arguments { get; init; }
        public Action<string>? OnOutputData { get; init; }
        public Action<string>? OnErrorData { get; init; }
        public Action<int>? OnStart { get; init; }
        public Action<int>? OnStop { get; init; }
        public bool KillEntireProcessTree { get; init; } = true;
        public bool ThrowOnNonZeroReturnCode { get; init; } = true;

        public ProcessSpec(string executablePath)
        {
            ExecutablePath = executablePath;
        }
    }
    internal static partial class ProcessUtil
    {
        #region Native Methods

        [LibraryImport("libc", SetLastError = true, EntryPoint = "kill")]
        private static partial int sys_kill(int pid, int sig);

        #endregion

        private static readonly TimeSpan s_processExitTimeout = TimeSpan.FromSeconds(5);

        public static (Task<ProcessResult>, IAsyncDisposable) Run(ProcessSpec processSpec)
        {
            var process = new System.Diagnostics.Process()
            {
                StartInfo =
            {
                FileName = processSpec.ExecutablePath,
                WorkingDirectory = processSpec.WorkingDirectory ?? string.Empty,
                Arguments = processSpec.Arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            },
                EnableRaisingEvents = true
            };

            if (!processSpec.InheritEnv)
            {
                process.StartInfo.Environment.Clear();
            }

            foreach (var (key, value) in processSpec.EnvironmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }

            // Use a reset event to prevent output processing and exited events from running until OnStart is complete.
            // OnStart might have logic that sets up data structures that then are used by these events.
            var startupComplete = new ManualResetEventSlim(false);

            // Note: even though the child process has exited, its children may be alive and still producing output.
            // See https://github.com/dotnet/runtime/issues/29232#issuecomment-1451584094 for how this might affect waiting for process exit.
            // We are going to discard that (grandchild) output by checking process.HasExited.

            if (processSpec.OnOutputData != null)
            {
                process.OutputDataReceived += (_, e) =>
                {
                    startupComplete.Wait();

                    if (String.IsNullOrEmpty(e.Data))
                    {
                        return;
                    }

                    processSpec.OnOutputData.Invoke(e.Data);
                };
            }

            if (processSpec.OnErrorData != null)
            {
                process.ErrorDataReceived += (_, e) =>
                {
                    startupComplete.Wait();
                    if (String.IsNullOrEmpty(e.Data))
                    {
                        return;
                    }

                    processSpec.OnErrorData.Invoke(e.Data);
                };
            }

            var processLifetimeTcs = new TaskCompletionSource<ProcessResult>();

            try
            {
#if ASPIRE_EVENTSOURCE
            AspireEventSource.Instance.ProcessLaunchStart(processSpec.ExecutablePath, processSpec.Arguments ?? "");
#endif

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                processSpec.OnStart?.Invoke(process.Id);

                process.WaitForExitAsync().ContinueWith(t =>
                {
                    startupComplete.Wait();

                    processSpec.OnStop?.Invoke(process.ExitCode);

                    if (processSpec.ThrowOnNonZeroReturnCode && process.ExitCode != 0)
                    {
                        processLifetimeTcs.TrySetException(new InvalidOperationException(
                            $"Command {processSpec.ExecutablePath} {processSpec.Arguments} returned non-zero exit code {process.ExitCode}"));
                    }
                    else
                    {
                        processLifetimeTcs.TrySetResult(new ProcessResult(process.ExitCode));
                    }
                }, TaskScheduler.Default);
            }
            finally
            {
                startupComplete.Set(); // Allow output/error/exit handlers to start processing data.
#if ASPIRE_EVENTSOURCE
            AspireEventSource.Instance.ProcessLaunchStop(processSpec.ExecutablePath, processSpec.Arguments ?? "");
#endif
            }

            return (processLifetimeTcs.Task, new ProcessDisposable(process, processLifetimeTcs.Task, processSpec.KillEntireProcessTree));
        }

        private sealed class ProcessDisposable : IAsyncDisposable
        {
            private readonly System.Diagnostics.Process _process;
            private readonly Task _processLifetimeTask;
            private readonly bool _entireProcessTree;

            public ProcessDisposable(System.Diagnostics.Process process, Task processLifetimeTask, bool entireProcessTree)
            {
                _process = process;
                _processLifetimeTask = processLifetimeTask;
                _entireProcessTree = entireProcessTree;
            }

            public async ValueTask DisposeAsync()
            {
                if (_process.HasExited)
                {
                    return; // nothing to do
                }

                if (OperatingSystem.IsWindows())
                {
                    if (!_process.CloseMainWindow())
                    {
                        _process.Kill(_entireProcessTree);
                    }
                }
                else
                {
                    sys_kill(_process.Id, sig: 2); // SIGINT
                }

                await _processLifetimeTask.WaitAsync(s_processExitTimeout).ConfigureAwait(false);
                if (!_process.HasExited)
                {
                    // Always try to kill the entire process tree here if all of the above has failed.
                    _process.Kill(entireProcessTree: true);
                }
            }
        }
    }

    public class BuildEAVFWAppsLifecycleHook : IDistributedApplicationLifecycleHook
    {
        private readonly ResourceLoggerService _resourceLoggerService;
        private readonly ResourceNotificationService _resourceNotificationService;

        public BuildEAVFWAppsLifecycleHook(ResourceLoggerService resourceLoggerService, ResourceNotificationService resourceNotificationService)
        {
            _resourceLoggerService = resourceLoggerService;
            _resourceNotificationService = resourceNotificationService;
        }
        public static string CreateMd5ForFolder(string path)
        {
            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();

            MD5 md5 = MD5.Create();

            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];

                // hash path
                string relativePath = file.Substring(path.Length + 1);
                byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                byte[] contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }


        public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            foreach (var resource in appModel.Resources)
            {

                //REVIEW - annotation vs resources. resources show up in dashboard and has its own logging.

                if (resource.TryGetLastAnnotation(out EAVFWBuildAnnotation buildAnnotation))
                {
                    var metadata = buildAnnotation.ProjectResource.GetProjectMetadata();
                    var hash = CreateMd5ForFolder(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), "src"));
                    var oldhash = File.Exists(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt")) ?
                        File.ReadAllText(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt")) : null;
                    if (hash != oldhash)
                    {

                    }
                }

                
                if (resource is EavBuildResource eavBuildResource)
                {
                    var metadata = eavBuildResource.Project.GetProjectMetadata();
                    var hash = CreateMd5ForFolder(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), "src"));

                    var logger = _resourceLoggerService.GetLogger(eavBuildResource);
                     
                    var oldhash = File.Exists(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt")) ?
                        File.ReadAllText(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt")) : null;
                    
                    if (hash != oldhash)
                    {

                        var test = ProcessUtil.Run(new ProcessSpec("cmd")
                        {
                            WorkingDirectory = eavBuildResource.Workingdirectory,
                            Arguments = $"/c \"{eavBuildResource.Command} " + string.Join(" ", eavBuildResource.Arguments) + "\"",
                            InheritEnv = true,
                            OnOutputData = (data) => logger.LogInformation(data),
                            OnErrorData = (data) => logger.LogError(data),
                            OnStart = (pid) =>
                            {
                                logger.LogInformation($"Started process with pid {pid}");
                                _resourceNotificationService.PublishUpdateAsync(eavBuildResource,
                                    state => state with { State = state.State with { Text = "Building", Style = KnownResourceStateStyles.Info } });
                            },
                            OnStop = (exitcode) =>
                            {
                                logger.LogInformation($"Process exited with code {exitcode}");
                                _resourceNotificationService.PublishUpdateAsync(eavBuildResource,
                                   state => state with { ExitCode = exitcode, State = state.State with { Text = exitcode == 0 ? KnownResourceStates.Finished: KnownResourceStates.Exited, Style = exitcode == 0? KnownResourceStateStyles.Success:KnownResourceStateStyles.Error } });

                              

                                File.WriteAllText(Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt"), hash);
                            }

                        });
                    }
                    else
                    {
                        await _resourceNotificationService.PublishUpdateAsync(eavBuildResource,
                                state => state with { State = state.State with { Text = KnownResourceStates.Finished, Style = KnownResourceStateStyles.Success } });

                    }

                }
            }

           
        }
    }

    /// <summary>
    /// The lifecycle hook that monitors the EAVFW Model Project resources and publishes updates to the resource state.
    /// </summary>


    public class PublishEAVFWProjectLifecycleHook : IDistributedApplicationLifecycleHook
        
    {
        private readonly ResourceLoggerService _resourceLoggerService;
        private readonly ILogger<PublishEAVFWProjectLifecycleHook> _aspirelogger;
        private readonly ResourceNotificationService _resourceNotificationService;

        public PublishEAVFWProjectLifecycleHook(ResourceLoggerService resourceLoggerService, ILogger<PublishEAVFWProjectLifecycleHook> aspirelogger,
            ResourceNotificationService resourceNotificationService)
        {
            _resourceLoggerService = resourceLoggerService ?? throw new ArgumentNullException(nameof(resourceLoggerService));
            _aspirelogger = aspirelogger;
            _resourceNotificationService = resourceNotificationService ?? throw new ArgumentNullException(nameof(resourceNotificationService));
        }

        public async Task BeforeStartAsync(DistributedApplicationModel application, CancellationToken cancellationToken = default)
        {
            var _ = Task.Run(async () =>
            {


                foreach (var modelResource in application.Resources.OfType<EAVFWModelProjectResource>())
                {
                    int retryCount = 10;
                    int delay = 1000; // Initial delay in milliseconds

                    for (int i = 0; i < retryCount; i++)
                    {
                        try
                        {

                            var logger = _resourceLoggerService.GetLogger(modelResource);

                            var modelProjectPath = modelResource.GetModelPath();

                            if (!modelResource.TryGetLastAnnotation<TargetDatabaseResourceAnnotation>(out var targetDatabaseResourceAnnotation)){
                                return;
                            }

                            var targetDatabaseResourceName = targetDatabaseResourceAnnotation.TargetDatabaseResourceName;
                            var targetDatabaseResource = application.Resources.OfType<SqlServerDatabaseResource>().Single(r => r.Name == targetDatabaseResourceName);
                            var targetSQLServerResource = targetDatabaseResource.Parent;


                            if (!modelResource.TryGetLastAnnotation(out DatabaseCreatedAnnotation createdatabaseannovation))
                            {
                                modelResource.Annotations.Add(createdatabaseannovation = new DatabaseCreatedAnnotation());
                            }

                            if (!createdatabaseannovation.Success)
                            {


                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                    state => state with { State = new ResourceStateSnapshot($"Creating DB ${targetDatabaseResource.DatabaseName} - Attempt ${createdatabaseannovation.Attempt}", KnownResourceStateStyles.Info) });

                                try
                                {
                                    var serverConnectionString = await targetSQLServerResource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);


                                    using var conn = new SqlConnection(serverConnectionString);

                                    await conn.OpenAsync();

                                    SqlCommand cmd = conn.CreateCommand();
                                    cmd.CommandText = $"""
                                       IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{targetDatabaseResource.DatabaseName}')
                                       BEGIN
                                         CREATE DATABASE [{targetDatabaseResource.DatabaseName}];
                                         ALTER DATABASE [{targetDatabaseResource.DatabaseName}] SET RECOVERY SIMPLE;
                                       END
                                       """;
                                    await cmd.ExecuteNonQueryAsync();



                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot($"{targetDatabaseResource.DatabaseName} was created", KnownResourceStateStyles.Info) });

                                    createdatabaseannovation.Success = true;
                                }
                                catch (InvalidOperationException invalid)
                                {
                                    createdatabaseannovation.Attempt++;
                                    logger.LogWarning(invalid, "Transient error, properly due to endpoints not up yet. We are backing off and trying again.");
                                    throw;
                                }
                                catch (SqlException sqlexception)
                                {
                                    createdatabaseannovation.Attempt++;
                                    logger.LogWarning(sqlexception, "Transient error, properly due to sql server not ready yet. We are backing off and trying again");
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to create db");

                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error) });

                                    throw;
                                }

                            }

                            if (!modelResource.TryGetLastAnnotation(out EAVFWMigrationdAnnotation migrationannotation))
                            {
                                modelResource.Annotations.Add(migrationannotation = new EAVFWMigrationdAnnotation());
                            }

                            if (!migrationannotation.Success)
                            {

                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                  state => state with { State = new ResourceStateSnapshot("Creating Migrations", KnownResourceStateStyles.Info) });


                                try
                                {

                                    var connectionString = await targetDatabaseResource.ConnectionStringExpression.GetValueAsync(cancellationToken);
                                    var migrationCount = await GetMigrationsCountAsync(targetDatabaseResource, connectionString);
                                    if (migrationCount == 0)
                                    {
                                        await DoMigrationAsync(modelProjectPath , targetDatabaseResourceAnnotation, targetDatabaseResource, connectionString);
                                    }

                                   migrationannotation.Success = true;
                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                      state => state with { State = new ResourceStateSnapshot($"migrations was created", KnownResourceStateStyles.Info) });

                                }
                                catch (SqlException sqlexception)
                                {
                                    migrationannotation.Attempt++;
                                    logger.LogWarning(sqlexception, "Transient error, properly due to sql server not ready yet. We are backing off and trying again");
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to create migrations");

                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error) });


                                }
                            }

                            if (modelResource.TryGetLastAnnotation<CreateSigninTokenAnnotation>(out var tokenannotation)){
                                try
                                {
                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                              state => state with { State = new ResourceStateSnapshot("Creating signin link", KnownResourceStateStyles.Info) });

                                    foreach(var target in modelResource.Annotations.OfType<CreateSigninUrlAnnotation>())
                                    {
                                        var link = await tokenannotation.GenerateLink(target,cancellationToken);

                                        await _resourceNotificationService.PublishUpdateAsync(target.Target,
                                            state => state with
                                            {   
                                                Properties = [.. state.Properties, new ResourcePropertySnapshot("signinlink", link.Link)  ],
                                                 Urls = [.. state.Urls, new UrlSnapshot("signinlink", link.Link,true), new UrlSnapshot("signinlink", link.Link, false)],
                                                
                                            });

                                        if (target.Project.TryGetEndpoints(out var endpoints))
                                        {
                                            var targetlogger = _resourceLoggerService.GetLogger(target.Target);

                                            targetlogger.LogInformation("Sign in with: {singinlink}", endpoints?.FirstOrDefault()?.AllocatedEndpoint.UriString+ link.Link);
                                            _aspirelogger.LogInformation("Sign in to {project}: {singinlink}", target.Target.Name, endpoints?.FirstOrDefault()?.AllocatedEndpoint.UriString + link.Link);

                                        }
                                    }


                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot("signin link crated", KnownResourceStateStyles.Info) });  



                                }
                                catch (Exception ex)
                                {

                                }
                            }

                            await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                      state => state with { State = new ResourceStateSnapshot(KnownResourceStates.Finished, KnownResourceStateStyles.Success) });
                            return;
                        }
                        catch (Exception ex)
                        {
                            if (i == retryCount - 1)
                            {
                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                   state => state with { State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error) });
                            }
                            else
                            {
                                await Task.Delay(delay);
                                delay *= 2; // Exponential backoff
                            }
                        }

                    }



                }

            }, cancellationToken);

        }
        
        private static async Task DoMigrationAsync(string modelProjectPath, TargetDatabaseResourceAnnotation targetDatabaseResourceAnnotation, SqlServerDatabaseResource targetDatabaseResource, string connectionString)
        {
            var variablegenerator = new SQLClientParameterGenerator();
            var migrator = new SQLMigrationGenerator(variablegenerator, new ManifestPermissionGenerator(variablegenerator));

            var sqls = await migrator.GenerateSQL(Path.GetDirectoryName(modelProjectPath), true, targetDatabaseResourceAnnotation.SystemUsersTableName ?? "SystemUsers",
                o =>
                {
                    o.UseNetTopologySuite();
                });

            var replacements = new Dictionary<string, string>
            {
                ["DBName"] = targetDatabaseResource.DatabaseName,
                ["DBSchema"] = targetDatabaseResourceAnnotation.Schema,
                ["SystemAdminSecurityGroupId"] = targetDatabaseResourceAnnotation.InitialSystemSecurityGroupId,
                ["UserGuid"] = targetDatabaseResourceAnnotation.InitialIdentity.ToString(),
                ["UserName"] = targetDatabaseResourceAnnotation.InitialUsername ?? "Poul Kjeldager",
                ["UserEmail"] = targetDatabaseResourceAnnotation.InitialEmail,
                ["UserPrincipalName"] = targetDatabaseResourceAnnotation.UserPrincipalName ?? "PoulKjeldagerSorensen"

            };

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                var files = new[] { sqls.SQL, sqls.Permissions };
                await conn.OpenAsync();
                foreach (var file in files)
                {
                    var cmdText = variablegenerator.DoReplacements(file, replacements);

                    foreach (var sql in cmdText.Split("GO"))
                    {
                        using var cmd = conn.CreateCommand();

                        cmd.CommandText = sql.Trim();
                        //  await context.Context.Database.ExecuteSqlRawAsync(sql);

                        if (!string.IsNullOrEmpty(cmd.CommandText))
                        {
                            // logger.LogInformation("Executing Migration SQL:\n{mig}", cmd.CommandText);
                            var r = await cmd.ExecuteNonQueryAsync();
                            // Console.WriteLine("Rows changed: " + r);
                        }
                    }






                }

            }
        }

        private static async Task<int> GetMigrationsCountAsync(SqlServerDatabaseResource targetDatabaseResource, string connectionString)
        {
            var migrationCount = 0;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = $"""
                                       IF (EXISTS (SELECT *  FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '__MigrationsHistory'))
                                       BEGIN
                                          select count(*) from dbo.__MigrationsHistory
                                       END
                                       ELSE
                                       BEGIN
                                          select 0
                                       END
                                   """;
                migrationCount = (int) await cmd.ExecuteScalarAsync();

            }

            return migrationCount;
        }

        public Task AfterResourcesCreatedAsync(DistributedApplicationModel application, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

}