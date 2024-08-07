using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EAVFramework.OpenTelemetry
{
    public class EAVMetrics
    {
        private Counter<int> SigninStartedCounter { get; }
        private Counter<int> SigninFailedCounter { get; }
        private Counter<int> SigninSuccessCounter { get; }

#if NET8_0_OR_GREATER
        public EAVMetrics(IMeterFactory meterFactory, IConfiguration configuration)
        {

            var meter = meterFactory.Create(configuration.GetValue<string>("EAVFW:Metrics:MeterName","EAVFW.Auth.Signin"));

            SigninStartedCounter = meter.CreateCounter<int>("signin-started", "Signin");
            SigninSuccessCounter = meter.CreateCounter<int>("signin-success", "Signin");
            SigninFailedCounter = meter.CreateCounter<int>("signin-failed", "Signin");

        }
#endif
        public void StartSignup(string authschema) => SigninStartedCounter?.Add(1,new TagList { { "schema", authschema } });
        public void SigninSuccess(string authschema) => SigninSuccessCounter?.Add(1, new TagList { { "schema", authschema } });
        public void SigninFailed(string authschema) => SigninFailedCounter?.Add(1, new TagList { { "schema", authschema } });

    }
}
