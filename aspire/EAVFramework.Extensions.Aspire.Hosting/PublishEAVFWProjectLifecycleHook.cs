using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using EAVFW.Extensions.Manifest.SDK;
using EAVFW.Extensions.Manifest.SDK.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

    public class EavNpmInstallResource : Resource, IResourceWithWaitSupport
    {
        public EavNpmInstallResource(string name, string workingDirectory) : base(name)
        {
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; }
        public ProjectResource Project { get; set; }
    }

    public class EavBuildResource : Resource, IResourceWithWaitSupport
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
        public ProjectResource Project { get; set; }
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
        private readonly ILogger<BuildEAVFWAppsLifecycleHook> _logger;

        public BuildEAVFWAppsLifecycleHook(ResourceLoggerService resourceLoggerService, ResourceNotificationService resourceNotificationService, ILogger<BuildEAVFWAppsLifecycleHook> logger)
        {
            _resourceLoggerService = resourceLoggerService;
            _resourceNotificationService = resourceNotificationService;
            _logger = logger;
        }
        public static string CreateMd5ForFolder(string path)
        {
            if (!Directory.Exists(path))
                return null;

            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();

            if (files.Count == 0)
                return null;

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


        private static async Task<ProcessResult> RunShellCommand(string command, string workingDirectory, ILogger logger, string logPrefix)
        {
            string shellExecutable;
            string shellArgument;

            if (OperatingSystem.IsWindows())
            {
                shellExecutable = "cmd";
                shellArgument = $"/c \"{command}\"";
            }
            else
            {
                shellExecutable = "/bin/bash";
                shellArgument = $"-c \"{command}\"";
            }

            logger.LogInformation("[{LogPrefix}] Running: {Command} in {WorkingDir}", logPrefix, command, workingDirectory);

            var (processTask, disposable) = ProcessUtil.Run(new ProcessSpec(shellExecutable)
            {
                WorkingDirectory = workingDirectory,
                Arguments = shellArgument,
                InheritEnv = true,
                ThrowOnNonZeroReturnCode = false,
                OnOutputData = (data) => logger.LogInformation("[{LogPrefix}] {Data}", logPrefix, data),
                OnErrorData = (data) => logger.LogError("[{LogPrefix} ERROR] {Data}", logPrefix, data),
                OnStart = (pid) => logger.LogInformation("[{LogPrefix}] Process started with PID {Pid}", logPrefix, pid),
                OnStop = (exitcode) => logger.LogInformation("[{LogPrefix}] Process exited with code {ExitCode}", logPrefix, exitcode)
            });

            var result = await processTask;
            await disposable.DisposeAsync();
            return result;
        }

        public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("BuildEAVFWAppsLifecycleHook: Starting BeforeStartAsync. OS: {OS}, IsWindows: {IsWindows}",
                Environment.OSVersion, OperatingSystem.IsWindows());

            foreach (var resource in appModel.Resources)
            {

                //REVIEW - annotation vs resources. resources show up in dashboard and has its own logging.

                if (resource.TryGetLastAnnotation(out EAVFWBuildAnnotation buildAnnotation))
                {
                    _logger.LogInformation("Found EAVFWBuildAnnotation for resource {ResourceName}", resource.Name);
                    var metadata = buildAnnotation.ProjectResource.GetProjectMetadata();
                    var srcPath = Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), "src");
                    _logger.LogInformation("Calculating hash for {SrcPath}", srcPath);

                    var hash = CreateMd5ForFolder(srcPath);
                    var hashFilePath = Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt");
                    var oldhash = File.Exists(hashFilePath) ?
                        File.ReadAllText(hashFilePath) : null;

                    _logger.LogInformation("Hash comparison - Current: {CurrentHash}, Previous: {PreviousHash}, Changed: {Changed}",
                        hash, oldhash ?? "none", hash != oldhash);

                    if (hash != oldhash)
                    {
                        _logger.LogInformation("Hash mismatch detected, build will be required for {ResourceName}", resource.Name);
                    }
                }

                if (resource is EavNpmInstallResource npmInstallResource)
                {
                    _logger.LogInformation("Processing EavNpmInstallResource: {ResourceName}", npmInstallResource.Name);
                    var logger = _resourceLoggerService.GetLogger(npmInstallResource);

                    try
                    {
                        // Step 0: If npm link is configured, prepare the monorepo first
                        var npmLinkAnnotation = npmInstallResource.Project
                            .Annotations.OfType<EAVFWNpmLinkAnnotation>().FirstOrDefault();

                        if (npmLinkAnnotation != null)
                        {
                            logger.LogInformation("npm link configured, preparing monorepo at {MonorepoRoot}", npmLinkAnnotation.MonorepoRoot);

                            await _resourceNotificationService.PublishUpdateAsync(npmInstallResource,
                                state => state with { State = state.State with { Text = "Installing monorepo deps", Style = KnownResourceStateStyles.Info } });

                            var monorepoInstallResult = await RunShellCommand(
                                "npm install --force --ignore-scripts", npmLinkAnnotation.MonorepoRoot, logger, "NPM LINK MONOREPO INSTALL");

                            if (monorepoInstallResult.ExitCode != 0)
                            {
                                logger.LogWarning("[EAVFW NPM LINK] Monorepo npm install failed with exit code {ExitCode}, continuing anyway", monorepoInstallResult.ExitCode);
                            }

                            await _resourceNotificationService.PublishUpdateAsync(npmInstallResource,
                                state => state with { State = state.State with { Text = "Registering package links", Style = KnownResourceStateStyles.Info } });

                            var linkSourceResult = await RunShellCommand(
                                "npm run link", npmLinkAnnotation.MonorepoRoot, logger, "NPM LINK SOURCE");

                            if (linkSourceResult.ExitCode != 0)
                            {
                                logger.LogWarning("[EAVFW NPM LINK] npm run link in monorepo failed with exit code {ExitCode}", linkSourceResult.ExitCode);
                            }
                            else
                            {
                                logger.LogInformation("[EAVFW NPM LINK] Monorepo package links registered successfully");
                            }
                        }

                        // Step 1: Run npm install in project working directory
                        logger.LogInformation("Starting npm install in {WorkingDir}", npmInstallResource.WorkingDirectory);

                        await _resourceNotificationService.PublishUpdateAsync(npmInstallResource,
                            state => state with { State = state.State with { Text = "Installing", Style = KnownResourceStateStyles.Info } });

                        var result = await RunShellCommand("npm install --force", npmInstallResource.WorkingDirectory, logger, "NPM INSTALL");

                        if (result.ExitCode == 0)
                        {
                            // Step 2: If npm link is configured, consume the symlinks in the project
                            if (npmLinkAnnotation != null)
                            {
                                logger.LogInformation("Setting up npm link from {MonorepoRoot}", npmLinkAnnotation.MonorepoRoot);

                                await _resourceNotificationService.PublishUpdateAsync(npmInstallResource,
                                    state => state with { State = state.State with { Text = "Linking packages", Style = KnownResourceStateStyles.Info } });

                                string linkCommand = "npm link --force @eavfw/apps @eavfw/next @eavfw/expressions @eavfw/manifest @eavfw/hooks @eavfw/forms @eavfw/utils";
                                var consumeResult = await RunShellCommand(linkCommand, npmInstallResource.WorkingDirectory, logger, "NPM LINK CONSUME");

                                if (consumeResult.ExitCode == 0)
                                    logger.LogInformation("[EAVFW NPM LINK READY] npm link completed successfully");
                                else
                                    logger.LogWarning("[EAVFW NPM LINK FAILED] npm link consume failed with exit code {ExitCode}", consumeResult.ExitCode);
                            }

                            logger.LogInformation("[EAVFW NPM INSTALL READY] npm install completed successfully for {ResourceName}", npmInstallResource.Name);
                            await _resourceNotificationService.PublishUpdateAsync(npmInstallResource,
                                state => state with {
                                    ExitCode = 0,
                                    State = state.State with { Text = KnownResourceStates.Finished, Style = KnownResourceStateStyles.Success }
                                });
                        }
                        else
                        {
                            logger.LogError("[EAVFW NPM INSTALL FAILED] npm install failed with exit code {ExitCode} for {ResourceName}", result.ExitCode, npmInstallResource.Name);
                            await _resourceNotificationService.PublishUpdateAsync(npmInstallResource,
                                state => state with {
                                    ExitCode = result.ExitCode,
                                    State = state.State with { Text = KnownResourceStates.Exited, Style = KnownResourceStateStyles.Error }
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during npm install for {ResourceName}: {Message}", npmInstallResource.Name, ex.Message);
                        await _resourceNotificationService.PublishUpdateAsync(npmInstallResource,
                            state => state with { State = state.State with { Text = "Install error", Style = KnownResourceStateStyles.Error } });
                    }
                }

                if (resource is EavBuildResource eavBuildResource)
                {
                    _logger.LogInformation("Processing EavBuildResource: {ResourceName}", eavBuildResource.Name);
                    var logger = _resourceLoggerService.GetLogger(eavBuildResource);

                    try
                    {
                        var metadata = eavBuildResource.Project.GetProjectMetadata();
                        logger.LogInformation("Project metadata - Path: {ProjectPath}", metadata.ProjectPath);

                        var srcPath = Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), "src");
                        logger.LogInformation("Checking source directory: {SrcPath}, Exists: {Exists}",
                            srcPath, Directory.Exists(srcPath));

                        if (!Directory.Exists(srcPath))
                        {
                            logger.LogError("Source directory does not exist: {SrcPath}. Cannot proceed with build.", srcPath);
                            await _resourceNotificationService.PublishUpdateAsync(eavBuildResource,
                                state => state with { State = state.State with { Text = "Source directory missing", Style = KnownResourceStateStyles.Error } });
                            continue;
                        }

                        logger.LogInformation("Calculating MD5 hash for source directory...");
                        var hash = CreateMd5ForFolder(srcPath);
                        logger.LogInformation("MD5 hash calculated: {Hash}", hash);

                        var hashFilePath = Path.Combine(Path.GetDirectoryName(metadata.ProjectPath), ".next/buildhash.txt");
                        var oldhash = File.Exists(hashFilePath) ?
                            File.ReadAllText(hashFilePath) : null;

                        logger.LogInformation("Previous build hash: {OldHash}", oldhash ?? "none (first build)");
                        logger.LogInformation("Build required: {BuildRequired}", hash != oldhash);

                        if (hash != oldhash)
                        {
                            logger.LogInformation("Starting build process...");
                            logger.LogInformation("Working directory: {WorkingDir}", eavBuildResource.Workingdirectory);
                            logger.LogInformation("Command: {Command}", eavBuildResource.Command);
                            logger.LogInformation("Arguments: {Arguments}", string.Join(" ", eavBuildResource.Arguments));

                            // Platform-specific shell configuration
                            string shellExecutable;
                            string shellArgument;
                            string buildCommand = $"{eavBuildResource.Command} " + string.Join(" ", eavBuildResource.Arguments);

                            if (OperatingSystem.IsWindows())
                            {
                                shellExecutable = "cmd";
                                shellArgument = $"/c \"{buildCommand}\"";
                                logger.LogInformation("Platform: Windows, Shell: cmd.exe");
                            }
                            else
                            {
                                shellExecutable = "/bin/bash";
                                shellArgument = $"-c \"{buildCommand}\"";
                                logger.LogInformation("Platform: Unix/Linux, Shell: /bin/bash");
                            }

                            logger.LogInformation("Full command: {Shell} {Arguments}", shellExecutable, shellArgument);

                            logger.LogInformation("Launching build process with {Shell}...", shellExecutable);
                            var test = ProcessUtil.Run(new ProcessSpec(shellExecutable)
                            {
                                WorkingDirectory = eavBuildResource.Workingdirectory,
                                Arguments = shellArgument,
                                InheritEnv = true,
                                EnvironmentVariables = { ["NODE_OPTIONS"] = "--max-old-space-size=2048" },
                                OnOutputData = (data) => logger.LogInformation("[BUILD OUTPUT] {Data}", data),
                                OnErrorData = (data) => logger.LogError("[BUILD ERROR] {Data}", data),
                                OnStart = (pid) =>
                                {
                                    logger.LogInformation("Build process started with PID {Pid}", pid);
                                    _resourceNotificationService.PublishUpdateAsync(eavBuildResource,
                                        state => state with { State = state.State with { Text = "Building", Style = KnownResourceStateStyles.Info } });
                                },
                                OnStop = (exitcode) =>
                                {
                                    logger.LogInformation("Build process exited with code {ExitCode}", exitcode);

                                    if (exitcode == 0)
                                    {
                                        logger.LogInformation("[EAVFW BUILD READY] Build completed successfully for {ResourceName}", eavBuildResource.Name);
                                        logger.LogInformation("Build completed successfully. Saving hash to {HashFile}", hashFilePath);
                                        try
                                        {
                                            var hashDir = Path.GetDirectoryName(hashFilePath);
                                            if (!Directory.Exists(hashDir))
                                            {
                                                Directory.CreateDirectory(hashDir);
                                                logger.LogInformation("Created directory for hash file: {HashDir}", hashDir);
                                            }
                                            File.WriteAllText(hashFilePath, hash);
                                            logger.LogInformation("Build hash saved successfully");
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogError(ex, "Failed to save build hash to {HashFile}", hashFilePath);
                                        }
                                    }
                                    else
                                    {
                                        logger.LogError("[EAVFW BUILD FAILED] Build failed with exit code {ExitCode} for {ResourceName}", exitcode, eavBuildResource.Name);
                                        logger.LogError("Build failed with exit code {ExitCode}", exitcode);
                                    }

                                    _resourceNotificationService.PublishUpdateAsync(eavBuildResource,
                                       state => state with {
                                           ExitCode = exitcode,
                                           State = state.State with {
                                               Text = exitcode == 0 ? KnownResourceStates.Finished : KnownResourceStates.Exited,
                                               Style = exitcode == 0 ? KnownResourceStateStyles.Success : KnownResourceStateStyles.Error
                                           }
                                       });
                                }

                            });

                            logger.LogInformation("Build process launched. Waiting for completion...");
                        }
                        else
                        {
                            logger.LogInformation("[EAVFW BUILD READY] Build skipped (unchanged) for {ResourceName}", eavBuildResource.Name);
                            logger.LogInformation("Source code unchanged (hash match). Skipping build.");
                            await _resourceNotificationService.PublishUpdateAsync(eavBuildResource,
                                    state => state with { State = state.State with { Text = KnownResourceStates.Finished, Style = KnownResourceStateStyles.Success } });

                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing build for {ResourceName}: {Message}", eavBuildResource.Name, ex.Message);
                        await _resourceNotificationService.PublishUpdateAsync(eavBuildResource,
                            state => state with { State = state.State with { Text = "Build error", Style = KnownResourceStateStyles.Error } });
                    }

                }
            }

            _logger.LogInformation("[EAVFW BUILD HOOK READY] BuildEAVFWAppsLifecycleHook completed");
            _logger.LogInformation("BuildEAVFWAppsLifecycleHook: Completed BeforeStartAsync");

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

        public Task BeforeStartAsync(DistributedApplicationModel application, CancellationToken cancellationToken = default)
        {
            var _ = Task.Run(async () =>
            {
                _aspirelogger.LogInformation("PublishEAVFWProjectLifecycleHook: Starting BeforeStartAsync");

                foreach (var modelResource in application.Resources.OfType<EAVFWModelProjectResource>())
                {
                    int retryCount = 10;
                    int delay = 1000; // Initial delay in milliseconds

                    for (int i = 0; i < retryCount; i++)
                    {
                        try
                        {

                            var logger = _resourceLoggerService.GetLogger(modelResource);
                            logger.LogInformation("Processing model resource: {ResourceName} (Attempt {Attempt}/{MaxAttempts})", modelResource.Name, i + 1, retryCount);

                            var modelProjectPath = modelResource.GetModelPath();
                            logger.LogInformation("Model project path: {ModelPath}", modelProjectPath);

                            if (!modelResource.TryGetLastAnnotation<TargetDatabaseResourceAnnotation>(out var targetDatabaseResourceAnnotation))
                            {
                                logger.LogWarning("No TargetDatabaseResourceAnnotation found for resource {ResourceName}. Skipping.", modelResource.Name);
                                return;
                            }

                            var targetDatabaseResourceName = targetDatabaseResourceAnnotation.TargetDatabaseResourceName;
                            logger.LogInformation("Target database resource name: {DatabaseResourceName}", targetDatabaseResourceName);

                            var targetDatabaseResource = application.Resources.OfType<SqlServerDatabaseResource>().Single(r => r.Name == targetDatabaseResourceName);
                            logger.LogInformation("Target database: {DatabaseName}", targetDatabaseResource.DatabaseName);

                            var targetSQLServerResource = targetDatabaseResource.Parent;
                            logger.LogInformation("Target SQL Server resource: {ServerResourceName}", targetSQLServerResource.Name);


                            if (!modelResource.TryGetLastAnnotation(out DatabaseCreatedAnnotation createdatabaseannovation))
                            {
                                modelResource.Annotations.Add(createdatabaseannovation = new DatabaseCreatedAnnotation());
                            }

                            if (!createdatabaseannovation.Success)
                            {

                                logger.LogInformation("Starting database creation for {DatabaseName} (Attempt {Attempt})", targetDatabaseResource.DatabaseName, createdatabaseannovation.Attempt);
                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                    state => state with { State = new ResourceStateSnapshot($"Creating DB {targetDatabaseResource.DatabaseName} - Attempt {createdatabaseannovation.Attempt}", KnownResourceStateStyles.Info) });

                                try
                                {
                                    logger.LogInformation("Retrieving SQL Server connection string for {ServerName}", targetSQLServerResource.Name);
                                    var serverConnectionString = await targetSQLServerResource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);

                                    // Log connection string (mask password for security)
                                    var sanitizedConnectionString = new SqlConnectionStringBuilder(serverConnectionString) { Password = "***" }.ConnectionString;
                                    logger.LogInformation("Connection string (sanitized): {ConnectionString}", sanitizedConnectionString);

                                    logger.LogInformation("Opening connection to SQL Server...");
                                    using var conn = new SqlConnection(serverConnectionString);
                                    await conn.OpenAsync();
                                    logger.LogInformation("Connection opened successfully");

                                    SqlCommand cmd = conn.CreateCommand();
                                    cmd.CommandText = $"""
                                       IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{targetDatabaseResource.DatabaseName}')
                                       BEGIN
                                         CREATE DATABASE [{targetDatabaseResource.DatabaseName}];
                                         ALTER DATABASE [{targetDatabaseResource.DatabaseName}] SET RECOVERY SIMPLE;
                                       END
                                       """;

                                    logger.LogInformation("Executing database creation command for {DatabaseName}", targetDatabaseResource.DatabaseName);
                                    await cmd.ExecuteNonQueryAsync();
                                    logger.LogInformation("Database {DatabaseName} created successfully", targetDatabaseResource.DatabaseName);
                                    logger.LogInformation("[EAVFW DB CREATE READY] Database {DatabaseName} created successfully", targetDatabaseResource.DatabaseName);

                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot($"{targetDatabaseResource.DatabaseName} was created", KnownResourceStateStyles.Info) });

                                    createdatabaseannovation.Success = true;
                                }
                                catch (InvalidOperationException invalid)
                                {
                                    createdatabaseannovation.Attempt++;
                                    logger.LogWarning(invalid, "Database creation failed with InvalidOperationException (Attempt {Attempt}). Endpoints may not be ready yet. Database: {DatabaseName}, Server: {ServerName}",
                                        createdatabaseannovation.Attempt, targetDatabaseResource.DatabaseName, targetSQLServerResource.Name);
                                    throw;
                                }
                                catch (SqlException sqlexception)
                                {
                                    createdatabaseannovation.Attempt++;
                                    logger.LogWarning(sqlexception, "Database creation failed with SqlException (Attempt {Attempt}). SQL Server may not be ready. Error Number: {ErrorNumber}, State: {State}, Database: {DatabaseName}",
                                        createdatabaseannovation.Attempt, sqlexception.Number, sqlexception.State, targetDatabaseResource.DatabaseName);
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to create database {DatabaseName}. Exception type: {ExceptionType}, Message: {Message}",
                                        targetDatabaseResource.DatabaseName, ex.GetType().Name, ex.Message);

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
                                logger.LogInformation("Starting migration process for {DatabaseName} (Attempt {Attempt})", targetDatabaseResource.DatabaseName, migrationannotation.Attempt);
                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                  state => state with { State = new ResourceStateSnapshot("Creating Migrations", KnownResourceStateStyles.Info) });


                                try
                                {
                                    logger.LogInformation("Retrieving connection string for database {DatabaseName}", targetDatabaseResource.DatabaseName);
                                    var connectionString = await targetDatabaseResource.ConnectionStringExpression.GetValueAsync(cancellationToken);

                                    logger.LogInformation("Checking existing migrations count for {DatabaseName}", targetDatabaseResource.DatabaseName);
                                    var migrationCount = await GetMigrationsCountAsync(targetDatabaseResource, connectionString, logger);
                                    logger.LogInformation("Found {MigrationCount} existing migrations in {DatabaseName}", migrationCount, targetDatabaseResource.DatabaseName);

                                    if (migrationCount == 0)
                                    {
                                        logger.LogInformation("No existing migrations found. Starting migration generation for {DatabaseName}", targetDatabaseResource.DatabaseName);
                                        await DoMigrationAsync(modelProjectPath, targetDatabaseResourceAnnotation, targetDatabaseResource, connectionString, logger);
                                        logger.LogInformation("Migration completed successfully for {DatabaseName}", targetDatabaseResource.DatabaseName);
                                    }
                                    else
                                    {
                                        logger.LogInformation("Skipping migration - {MigrationCount} migrations already exist in {DatabaseName}", migrationCount, targetDatabaseResource.DatabaseName);
                                    }

                                    migrationannotation.Success = true;
                                    logger.LogInformation("[EAVFW MIGRATION READY] Migrations applied successfully to {DatabaseName}", targetDatabaseResource.DatabaseName);
                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                      state => state with { State = new ResourceStateSnapshot($"migrations was created", KnownResourceStateStyles.Info) });

                                }
                                catch (SqlException sqlexception)
                                {
                                    migrationannotation.Attempt++;
                                    logger.LogWarning(sqlexception, "Migration failed with SqlException (Attempt {Attempt}). Error Number: {ErrorNumber}, State: {State}, Database: {DatabaseName}",
                                        migrationannotation.Attempt, sqlexception.Number, sqlexception.State, targetDatabaseResource.DatabaseName);
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to create migrations for {DatabaseName}. Exception type: {ExceptionType}, Message: {Message}",
                                        targetDatabaseResource.DatabaseName, ex.GetType().Name, ex.Message);

                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error) });


                                }
                            }

                            if (modelResource.TryGetLastAnnotation<CreateSigninTokenAnnotation>(out var tokenannotation))
                            {
                                try
                                {
                                    logger.LogInformation("Starting signin link creation for {ResourceName}", modelResource.Name);
                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                              state => state with { State = new ResourceStateSnapshot("Creating signin link", KnownResourceStateStyles.Info) });

                                    var signinUrlAnnotations = modelResource.Annotations.OfType<CreateSigninUrlAnnotation>().ToList();
                                    logger.LogInformation("Found {Count} signin URL annotations to process", signinUrlAnnotations.Count);

                                    foreach (var target in signinUrlAnnotations)
                                    {
                                        logger.LogInformation("Generating signin link for target {TargetName}", target.Target.Name);
                                        var link = await tokenannotation.GenerateLink(target, cancellationToken);
                                        logger.LogInformation("Generated signin link: {Link}", link.Link);

                                        await _resourceNotificationService.PublishUpdateAsync(target.Target,
                                            state => state with
                                            {
                                                Properties = [.. state.Properties, new ResourcePropertySnapshot("signinlink", link.Link)],
                                                Urls = [.. state.Urls, new UrlSnapshot("signinlink", link.Link, true), new UrlSnapshot("signinlink", link.Link, false)],

                                            });

                                        if (target.Project.TryGetEndpoints(out var endpoints))
                                        {
                                            var targetlogger = _resourceLoggerService.GetLogger(target.Target);
                                            var fullSigninUrl = endpoints?.FirstOrDefault()?.AllocatedEndpoint.UriString + link.Link;

                                            targetlogger.LogInformation("Sign in with: {singinlink}", fullSigninUrl);
                                            _aspirelogger.LogInformation("Sign in to {project}: {singinlink}", target.Target.Name, fullSigninUrl);
                                            logger.LogInformation("Published signin link for {TargetName}: {Url}", target.Target.Name, fullSigninUrl);

                                        }
                                        else
                                        {
                                            logger.LogWarning("No endpoints found for target {TargetName}", target.Target.Name);
                                        }
                                    }


                                    logger.LogInformation("[EAVFW SIGNIN READY] Signin link created for {ResourceName}", modelResource.Name);
                                    await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                        state => state with { State = new ResourceStateSnapshot("signin link created", KnownResourceStateStyles.Info) });

                                    logger.LogInformation("Signin link creation completed for {ResourceName}", modelResource.Name);

                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to create signin link for {ResourceName}. Exception type: {ExceptionType}, Message: {Message}",
                                        modelResource.Name, ex.GetType().Name, ex.Message);
                                }
                            }
                            else
                            {
                                logger.LogInformation("No CreateSigninTokenAnnotation found for {ResourceName}, skipping signin link creation", modelResource.Name);
                            }

                            logger.LogInformation("[EAVFW MODEL READY] All operations completed successfully for {ResourceName}", modelResource.Name);
                            logger.LogInformation("Successfully completed all operations for {ResourceName}", modelResource.Name);
                            await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                      state => state with { State = new ResourceStateSnapshot(KnownResourceStates.Finished, KnownResourceStateStyles.Success) });
                            return;
                        }
                        catch (Exception ex)
                        {
                            var logger = _resourceLoggerService.GetLogger(modelResource);

                            if (i == retryCount - 1)
                            {
                                logger.LogError(ex, "Failed to process {ResourceName} after {RetryCount} attempts. Exception type: {ExceptionType}, Message: {Message}",
                                    modelResource.Name, retryCount, ex.GetType().Name, ex.Message);
                                _aspirelogger.LogError("[EAVFW MODEL FAILED] Resource {ResourceName} failed after {RetryCount} attempts", modelResource.Name, retryCount);
                                _aspirelogger.LogError(ex, "FINAL FAILURE: Resource {ResourceName} failed after {RetryCount} attempts", modelResource.Name, retryCount);

                                await _resourceNotificationService.PublishUpdateAsync(modelResource,
                                   state => state with { State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error) });
                            }
                            else
                            {
                                logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed for {ResourceName}. Retrying in {Delay}ms. Exception type: {ExceptionType}, Message: {Message}",
                                    i + 1, retryCount, modelResource.Name, delay, ex.GetType().Name, ex.Message);

                                await Task.Delay(delay);
                                delay *= 2; // Exponential backoff

                                logger.LogInformation("Retrying {ResourceName} (Attempt {NextAttempt}/{MaxAttempts}) with {NewDelay}ms delay",
                                    modelResource.Name, i + 2, retryCount, delay);
                            }
                        }

                    }



                }

                _aspirelogger.LogInformation("PublishEAVFWProjectLifecycleHook: Completed processing all EAVFWModelProjectResource resources");

            }, cancellationToken);
            return Task.CompletedTask;
        }

        private static async Task DoMigrationAsync(string modelProjectPath, TargetDatabaseResourceAnnotation targetDatabaseResourceAnnotation, SqlServerDatabaseResource targetDatabaseResource, string connectionString, ILogger logger)
        {
            logger.LogInformation("Starting DoMigrationAsync for database {DatabaseName}, Model path: {ModelPath}", targetDatabaseResource.DatabaseName, modelProjectPath);

            var variablegenerator = new SQLClientParameterGenerator();
            var migrator = new SQLMigrationGenerator(variablegenerator, new ManifestPermissionGenerator(variablegenerator));

            logger.LogInformation("Generating SQL migrations from manifest at {ManifestPath}", Path.GetDirectoryName(modelProjectPath));
            var sqls = await migrator.GenerateSQL(Path.GetDirectoryName(modelProjectPath), true, targetDatabaseResourceAnnotation.SystemUsersTableName ?? "SystemUsers",
                o =>
                {
                    o.UseNetTopologySuite();
                });
            logger.LogInformation("SQL migration generation completed");

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
            logger.LogInformation("Replacements configured: DBName={DBName}, DBSchema={DBSchema}, UserEmail={UserEmail}",
                replacements["DBName"], replacements["DBSchema"], replacements["UserEmail"]);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                var files = new[] { sqls.SQL, sqls.Permissions };
                logger.LogInformation("Opening connection to database {DatabaseName}", targetDatabaseResource.DatabaseName);
                await conn.OpenAsync();
                logger.LogInformation("Connection opened. Processing {FileCount} SQL files", files.Length);

                int fileIndex = 0;
                foreach (var file in files)
                {
                    fileIndex++;
                    var fileType = fileIndex == 1 ? "Schema" : "Permissions";
                    logger.LogInformation("Processing {FileType} SQL file ({FileIndex}/{FileCount})", fileType, fileIndex, files.Length);

                    var cmdText = variablegenerator.DoReplacements(file, replacements);
                    var sqlBatches = cmdText.Split("GO");
                    logger.LogInformation("Found {BatchCount} SQL batches in {FileType} file", sqlBatches.Length, fileType);

                    int batchIndex = 0;
                    foreach (var sql in sqlBatches)
                    {
                        batchIndex++;
                        using var cmd = conn.CreateCommand();

                        cmd.CommandText = sql.Trim();

                        if (!string.IsNullOrEmpty(cmd.CommandText))
                        {
                            logger.LogInformation("Executing {FileType} SQL batch {BatchIndex}/{BatchCount} ({Length} chars)",
                                fileType, batchIndex, sqlBatches.Length, cmd.CommandText.Length);
                            logger.LogDebug("SQL Command:\n{SQL}", cmd.CommandText);

                            var r = await cmd.ExecuteNonQueryAsync();
                            logger.LogInformation("Batch {BatchIndex} completed. Rows affected: {RowsAffected}", batchIndex, r);
                        }
                        else
                        {
                            logger.LogDebug("Skipping empty SQL batch {BatchIndex}", batchIndex);
                        }
                    }






                }

            }
        }

        private static async Task<int> GetMigrationsCountAsync(SqlServerDatabaseResource targetDatabaseResource, string connectionString, ILogger logger)
        {
            logger.LogInformation("Checking migrations count in database {DatabaseName}", targetDatabaseResource.DatabaseName);

            var migrationCount = 0;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                logger.LogInformation("Opening connection to check migrations in {DatabaseName}", targetDatabaseResource.DatabaseName);
                await conn.OpenAsync();
                logger.LogInformation("Connection opened for migrations check");

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
                logger.LogDebug("Executing migration count query");
                migrationCount = (int) await cmd.ExecuteScalarAsync();
                logger.LogInformation("Migration count query completed. Found {MigrationCount} migrations", migrationCount);

            }

            return migrationCount;
        }

        public Task AfterResourcesCreatedAsync(DistributedApplicationModel application, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

}