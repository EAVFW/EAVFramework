using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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

            var meter = meterFactory.Create(configuration.GetValue<string>("EAVFW:Metrics:MeterName","EAVFWMeter"));

            SigninStartedCounter = meter.CreateCounter<int>("signin-started", "Signin");
            SigninSuccessCounter = meter.CreateCounter<int>("signin-success", "Signin");
            SigninFailedCounter = meter.CreateCounter<int>("signin-failed", "Signin");

        }
#endif
        public void StartSignup() => SigninStartedCounter?.Add(1);
        public void SigninSuccess() => SigninSuccessCounter?.Add(1);
        public void SigninFailed() => SigninFailedCounter?.Add(1);

    }
}
