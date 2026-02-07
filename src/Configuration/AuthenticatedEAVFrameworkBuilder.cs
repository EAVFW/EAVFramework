using EAVFramework.Endpoints;
using EAVFramework.Extensions;
using EAVFramework.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace EAVFramework.Configuration
{
    public interface IEAVFrameworkTicketStore
    {
        public Task<TicketInformation> GetTicketInfoAsync(UnPersistTicketReuqest request);
        public Task PersistTicketAsync(PersistTicketRequest request);
    }
    public interface IEAVFrameworkTicketStore<TContext> : IEAVFrameworkTicketStore

    {

    }
    public interface IEAVFrameworkTicketStore<TContext,TSignin> : IEAVFrameworkTicketStore<TContext>
    {

    }
    public class EAVFrameworkTicketStore<TContext, TSignin> : IEAVFrameworkTicketStore, IEAVFrameworkTicketStore<TContext>, IEAVFrameworkTicketStore<TContext,TSignin>
        where TContext : DynamicContext
        where TSignin : DynamicEntity, ISigninRecord, new()
    {
        public async Task PersistTicketAsync(PersistTicketRequest request)
         
        {


            var ctx = request.ServiceProvider.GetRequiredService<EAVDBContext<TContext>>();
            var signin = await ctx.Set<TSignin>().FindAsync(request.HandleId);

            if (signin == null)
            {
                ctx.Context.Add(new TSignin
                {
                    Id = request.HandleId,
                    // Status = SigninStatus.Approved,
                    Properties = JsonConvert.SerializeObject(new object[] { new { type = "ticket", value = request.Ticket }, new { type = "redirectUri", value = request.RedirectUrl } }),
                    Provider = request.AuthProvider,
                    IdentityId = request.IdentityId
                });

                var result = await ctx.SaveChangesAsync(request.OwnerIdentity);
                
                if(result.Errors.Any())
                {
                    throw new InvalidOperationException("Error saving signin" + result.Errors.First().Error);
                }

            }
        }

        public async Task<TicketInformation> GetTicketInfoAsync(UnPersistTicketReuqest request)
             
        {


            var ctx = request.ServiceProvider.GetRequiredService<EAVDBContext<TContext>>();
            var signin = await ctx.Set<TSignin>().FindAsync(request.HandleId);
            if (signin == null)
                return null;


            var data = JToken.Parse(signin.Properties).ToDictionary(k => k.SelectToken("$.type")?.ToString(), v => v.SelectToken("$.value"));
            return new TicketInformation
            {
                Ticket = data["ticket"].ToObject<byte[]>(),
                RedirectUrl = data["redirectUri"]?.ToString(),
                IdentityId = signin.IdentityId.Value
            };

        }
    }
    public class AuthenticatedEAVFrameworkBuilder //: EAVFrameworkBuilder
    {
        private readonly IEAVFrameworkBuilder _eavFrameworkBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public AuthenticatedEAVFrameworkBuilder(IEAVFrameworkBuilder eavFrameworkBuilder)
        {
            _eavFrameworkBuilder = eavFrameworkBuilder;
        }

        public IServiceCollection Services => _eavFrameworkBuilder.Services;

 

        public AuthenticatedEAVFrameworkBuilder WithEAVSigninTicketStore<TContext, TSignin>()
             where TContext : DynamicContext
             where TSignin : DynamicEntity, ISigninRecord, new()
        {

          
            Services.AddScoped<IPasswordLessLinkGenerator>(sp => sp.GetService<PasswordLessLinkGenerator<TContext,TSignin>>());
            Services.AddScoped<IEAVFrameworkTicketStore>(sp => sp.GetRequiredService<IEAVFrameworkTicketStore<TContext, TSignin>>());
            Services.AddScoped<IEAVFrameworkTicketStore<TContext>>(sp => sp.GetService<IEAVFrameworkTicketStore<TContext, TSignin>>());
          //  Services.AddScoped<IEAVFrameworkTicketStore<TContext,TSignin>>(sp => sp.GetService<EAVFrameworkTicketStore<TContext, TSignin>>());




            return this;
        }

       




    }



    [EntityInterface(EntityKey = "*")]
    public interface ISigninRecord
    {
        Guid Id { get; set; }
        Guid? IdentityId { get; set; }
        String Properties { get; set; }
        String Provider { get; set; }
    }

   
}
