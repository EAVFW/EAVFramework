using EAVFramework.Endpoints;
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

        public AuthenticatedEAVFrameworkBuilder WithSigninStore<TContext, TSignin>()
               where TContext : DynamicContext
            where TSignin : DynamicEntity, ISigninRecord, new()
        {
            Services.Configure<EAVFrameworkOptions>(options =>
            {
                options.Authentication.UnPersistTicketAsync = GetTicketInfoAsync<TContext, TSignin>;
                options.Authentication.PersistTicketAsync = PersistTicketAsync<TContext, TSignin>;
                
            });


            return this;
        }

       

        private async Task PersistTicketAsync<TContext, TSignin>(PersistTicketRequest request)
            where TContext : DynamicContext
            where TSignin : DynamicEntity, ISigninRecord,new()
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


            }
        }

        private async Task<TicketInformation> GetTicketInfoAsync<TContext, TSignin>(UnPersistTicketReuqest request)
            where TContext : DynamicContext
            where TSignin : DynamicEntity, ISigninRecord
        {


            var ctx = request.ServiceProvider.GetRequiredService<EAVDBContext<TContext>>();
            var signin = await ctx.Set<TSignin>().FindAsync(request.HandleId);

            var data = JToken.Parse(signin.Properties).ToDictionary(k => k.SelectToken("$.type")?.ToString(), v => v.SelectToken("$.value"));
            return new TicketInformation
            {
                Ticket = data["ticket"].ToObject<byte[]>(),
                RedirectUrl = data["redirectUri"]?.ToString(),
                IdentityId = signin.IdentityId.Value
            };

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
