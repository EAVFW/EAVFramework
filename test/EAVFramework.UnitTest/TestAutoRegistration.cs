using EAVFramework.Configuration;
using EAVFramework.Endpoints;
using EAVFramework.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.UnitTest
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
            serviceCollection.AddTransient<PluginsAccesser<DynamicContext>>();

            var builder = new EAVFrameworkBuilder<DynamicContext>(serviceCollection);


            builder.AddPlugin<TestPlugin>();

            var sp = serviceCollection.BuildServiceProvider();
            var plugins = sp.GetRequiredService<PluginsAccesser<DynamicContext>>();


            Assert.AreEqual(2, plugins.Count());

        }

    }
}
