using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using DotNetDevOps.Extensions.EAVFramework.Extensions;
using System;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication
{
    public static class IApplicationBuilderExtensions
    {
        public static AuthenticationBuilder AddForwardedEasyAuth(this AuthenticationBuilder authBuilder, string schemaName = "EasyAuth", Action<EasyAuthForwardedAuthOptions> options=null)
        {
            authBuilder.AddScheme<EasyAuthForwardedAuthOptions, EasyAuthForwardedAuth>(schemaName, options ?? EmptyOptions);
            return authBuilder;
        }

        private static void EmptyOptions(EasyAuthForwardedAuthOptions options)
        {
            
        }

        public static T SetForwardedSubjectId<T>(this T builder,   params string[] authSchemas) where T : IApplicationBuilder
        {
            return builder.SetForwardedSubjectId("X-EasyAuth-UserID", authSchemas);
        }
        
        public static T SetForwardedSubjectId<T>(this T builder, string headerName= "X-EasyAuth-UserID", string[] authSchemas=null) where T: IApplicationBuilder
        {

            builder.Use(async (context, next) =>
            {

                if (authSchemas != null)
                {
                    foreach (var schema in authSchemas)
                    {
                        var auth = await context.AuthenticateAsync(schema);

                        if (auth.Succeeded && auth.Principal.FindFirstValue("sub") is string subject && subject.IsPresent())
                        {
                            context.Request.Headers.Add(headerName, subject);

                            break;
                        }

                    }
                }
                 

                await next().ConfigureAwait(false);
            });

            return builder;
        }
    }
 
}
