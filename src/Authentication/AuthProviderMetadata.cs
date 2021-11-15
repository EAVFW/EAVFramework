using System.Reflection;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication
{
    public class AuthProviderMetadata<T> where T : IEasyAuthProvider
    {
        private static readonly PropertyInfo prop = typeof(T).GetProperty("AuthenticationName");

        public string AuthenticationName()
        {
            return (string) prop.GetValue(null);
        }
    }
 
}
