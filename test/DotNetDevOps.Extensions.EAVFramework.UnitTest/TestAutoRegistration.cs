using DotNetDevOps.Extensions.EAVFramework.Configuration;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.UnitTest
{

    [PluginRegistration( EntityPluginExecution.PostOperation, EntityPluginOperation.Create)]
    [PluginRegistration(EntityPluginExecution.PostOperation, EntityPluginOperation.Update)]
    public class TestPlugin : IPlugin<DynamicContext, DynamicEntity>, IPluginRegistration
    {
        public Task Execute(PluginContext<DynamicContext, DynamicEntity> context)
        {
            throw new NotImplementedException();
        }
    }
    [TestClass]
    public class AutoRegistrationTests
    {


        [TestMethod]
        public async Task TestAutoRegistration()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<PluginsAccesser>();

            var builder = new EAVFrameworkBuilder(serviceCollection);


            builder.AddPlugin<TestPlugin>();

            var sp = serviceCollection.BuildServiceProvider();
            var plugins = sp.GetRequiredService<PluginsAccesser>();


            Assert.AreEqual(2, plugins.Count());

        }

    }
}
