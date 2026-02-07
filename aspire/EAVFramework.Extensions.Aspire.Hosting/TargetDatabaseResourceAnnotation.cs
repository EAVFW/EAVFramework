using Aspire.Hosting.ApplicationModel;
using EAVFramework.Configuration;
using EAVFramework.Endpoints;
using EAVFW.Extensions.SecurityModel;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EAVFramework.Extensions.Aspire.Hosting
{
    public record TargetDatabaseResourceAnnotation(string TargetDatabaseResourceName, SqlServerDatabaseResource TargetDatabaseResource) : IResourceAnnotation
    {
        public string Schema { get; set; } = "dbo";
        public Guid InitialIdentity { get;  set; }
        public string InitialEmail { get;  set; }
        public string UserPrincipalName { get;  set; }
        public string InitialUsername { get;  set; }
        public string SystemUsersTableName{get;set;} = "SystemUsers";
        public string InitialSystemSecurityGroupId { get;  set; } = "1b714972-8d0a-4feb-b166-08d93c6ae328";
    }
    public record CreateSigninUrlAnnotation(IResource Target, IResource Project) : IResourceAnnotation
    {
     
    }
    public abstract class CreateSigninTokenAnnotation() : IResourceAnnotation
    {
        public Action<IServiceCollection> ServiceCollectionExtender { get; set; }
        public Action<SqlServerDbContextOptionsBuilder> SqlExtender { get; set; }
        public abstract Task<LinkResult> GenerateLink(CreateSigninUrlAnnotation createSigninUrl,CancellationToken cancellationToken);
    }
    public class CreateSigninTokenAnnotation<TContext,TIdentity,TSignin> : CreateSigninTokenAnnotation
        where TContext : DynamicContext
        where TIdentity : DynamicEntity, IIdentity
        where TSignin : DynamicEntity, ISigninRecord, new()
    {
        private PasswordLessLinkGenerator<TContext, TSignin> _linkGenerator;
        public CreateSigninTokenAnnotation(EAVFWModelProjectResource eAVFWModelProjectResource)
        {
            _eAVFWModelProjectResource = eAVFWModelProjectResource;
        }
       
        private readonly EAVFWModelProjectResource _eAVFWModelProjectResource;

        public override async Task<LinkResult> GenerateLink(CreateSigninUrlAnnotation createSigninUrl, CancellationToken cancellationToken)
        {
            if (!_eAVFWModelProjectResource.TryGetLastAnnotation<TargetDatabaseResourceAnnotation>(out var targetdatabaes))
            {
                return null;
            }

            if (_linkGenerator == null)
            {
                _linkGenerator= await CreateGenerator(targetdatabaes, cancellationToken);

            }
            
            createSigninUrl.Target.TryGetEndpoints(out var endpoints);
            var url = endpoints?.FirstOrDefault()?.AllocatedEndpoint.UriString ?? "https://localhost:3000/";
            var link = await _linkGenerator.GenerateLink(targetdatabaes.InitialIdentity,
                new Dictionary<string, StringValues> { { "email", targetdatabaes.InitialEmail } }, url, CreateGuidFromString(url+targetdatabaes.InitialEmail));
            return link;
        }

        private async Task<PasswordLessLinkGenerator<TContext, TSignin>> CreateGenerator(TargetDatabaseResourceAnnotation targetdatabaes, CancellationToken cancellationToken)
        {
            var modelProjectPath = _eAVFWModelProjectResource.GetModelPath();

            var services = new ServiceCollection();

            var eav = services.AddEAVFrameworkBuilder<DynamicContext>(
                  schema: targetdatabaes.Schema,
                  connectionString: await targetdatabaes.TargetDatabaseResource.ConnectionStringExpression.GetValueAsync(cancellationToken))
                  .AddPluggableServices();
            services.Configure<EAVFrameworkOptions>(o =>
            {
                o.SystemAdministratorIdentity = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub",targetdatabaes.InitialSystemSecurityGroupId)
                                }, EAVFramework.Constants.DefaultCookieAuthenticationScheme));
            });
            eav.WithAuditFieldsPlugins<DynamicContext, TIdentity>();
            //   eav.AddAuthentication(o => { }).WithEAVSigninTicketStore<DynamicContext, TSignin>();
            ServiceCollectionExtender?.Invoke(services);
            //   services.AddCodeServices();
            //   services.AddSingleton<IMigrationManager, MigrationManager>();
            //   services.AddScoped(typeof(EAVDBContext<>));
            services.AddOptions<DynamicContextOptions>().Configure((o) =>
            {
                o.Manifests = new[] { JToken.Parse(File.ReadAllText(Path.Combine($"{Path.GetDirectoryName(modelProjectPath)}/obj/", "manifest.g.json"))) };
                o.Schema = targetdatabaes.Schema;
                o.EnableDynamicMigrations = true;
                o.Namespace = "eavfw";
                o.DTOAssembly = typeof(TIdentity).Assembly;

                o.DTOBaseClasses = new[] { typeof(BaseOwnerEntity<TIdentity>), typeof(BaseIdEntity<TIdentity>) };
            });

            var connectionstring = await targetdatabaes.TargetDatabaseResource.ConnectionStringExpression.GetValueAsync(cancellationToken);
            services.AddDbContext<DynamicContext>(optionsBuilder =>
            {
                optionsBuilder.UseSqlServer(connectionstring,
                    x =>
                    {
                        x.MigrationsHistoryTable("__MigrationsHistory", targetdatabaes.Schema)
                    .EnableRetryOnFailure();

                        SqlExtender?.Invoke(x);
                    }
                    );
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();


            }, Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient);
            var sp = services.BuildServiceProvider();

            return sp.GetRequiredService<PasswordLessLinkGenerator<TContext, TSignin>>();
        }

        private static Guid CreateGuidFromString(string input)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                byte[] guidBytes = new byte[16];
                Array.Copy(hashBytes, guidBytes, 16);
                return new Guid(guidBytes);
            }
        }
    }
}