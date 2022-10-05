using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework
{
    public static class Constants
    {
        public static class EndpointNames
        {
            public const string QueryRecords = "QueryRecords";

            public const string CreateRecord = "CreateRecord";
            public const string QueryEntityPermissions = nameof(QueryEntityPermissions);
            public const string RetrieveRecord = "RetrieveRecord";
            public const string PatchRecord = "PatchRecord";
            public const string DeleteRecord = "DeleteRecord";
        }

        public static class RoutePatterns
        {
         
          
            public const string QueryRecords = "/entities/{EntityCollectionSchemaName}";
            public const string CreateRecord = "/entities/{EntityCollectionSchemaName}/records";
            public const string QueryEntityPermissions = "/entities/{EntityCollectionSchemaName}/permissions";
            public const string RecordPattern = "/entities/{EntityCollectionSchemaName}/records/{RecordId}";
        }
        public static class RouteParams
        {
            public const string EntityCollectionSchemaNameRouteParam = "EntityCollectionSchemaName";
            public const string RecordIdRouteParam = "RecordId";
        }

       
        public const string DefaultCookieAuthenticationScheme = "eavfw";
        public const string ExternalCookieAuthenticationScheme = "eavfw.external";
        public const string DefaultCheckSessionCookieName = "eavfw.session";
        public const string DefaultLoginRedirectCookie = "eavauth.login";

        public static readonly TimeSpan DefaultCookieTimeSpan = TimeSpan.FromHours(10);


        public static class UIConstants
        {
            // the limit after which old messages are purged
            public const int CookieMessageThreshold = 2;

            public static class DefaultRoutePathParams
            {
                public const string Error = "errorId";
                public const string Login = "returnUrl";
                public const string Consent = "returnUrl";
                public const string Logout = "logoutId";
                public const string EndSessionCallback = "endSessionId";
                public const string Custom = "returnUrl";
                public const string UserCode = "userCode";
            }

            public static class DefaultRoutePaths
            {
                public const string Login = "/account/login";
                public const string LoginCallback = "/account/login/callback";
                public const string Logout = "/account/logout";
                public const string Consent = "/consent";
                public const string Error = "/home/error";
                public const string DeviceVerification = "/device";
            }
        }
    }
}
