using EAVFramework.Shared;
using EAVFramework.Shared.V2;
using EAVFramework.UnitTest.ManifestTests;
using EAVFW.Extensions.Manifest.SDK;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EAVFramework.UnitTest
{
    [EntityInterface(EntityKey = "Payment Provider Type")]
    public interface IPaymentProviderType
    {
        string Type { get; set; }
    }
    [EntityInterface(EntityKey = "Payment Provider")]
    [ConstraintMapping(EntityKey = "Payment Provider Type", ConstraintName = nameof(TType))]
    public interface IPaymentProvider<TType>
        where TType : DynamicEntity, IPaymentProviderType
    {
        TType PaymentProviderType { get; set; }
    }

    [EntityInterface(EntityKey = "Agreement")]
    [ConstraintMapping(EntityKey = "Payment Provider", ConstraintName = nameof(TProvider))]
    [ConstraintMapping(EntityKey = "Payment Provider Type", ConstraintName = nameof(TType))]
    public interface IAgreement<TProvider,TType>
       where TProvider : DynamicEntity, IPaymentProvider<TType>
       where TType : DynamicEntity, IPaymentProviderType
       
    {
          TProvider Provider { get; set; }
    }

    [TestClass]
    public class GenericTests : BaseManifestTests
    {

        [TestMethod]
        public void BuildContarints()
        {
            AppDomain myDomain = AppDomain.CurrentDomain;
            AssemblyName myAsmName = new AssemblyName("MyNamespace");

            var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
              AssemblyBuilderAccess.RunAndCollect);



            ModuleBuilder myModule =
              builder.DefineDynamicModule("MyNamespace.dll");


            //Arrage (Build IAgreement above)
            var baseTypeInterfacesbuilders = new ConcurrentDictionary<string, InterfaceShadowBuilder>();
            var IPaymentProviderType = baseTypeInterfacesbuilders.GetOrAdd("Test.IPaymentProviderType", name=> new InterfaceShadowBuilder(myModule, baseTypeInterfacesbuilders, name));
            var IPaymentProvider = baseTypeInterfacesbuilders.GetOrAdd("Test.IPaymentProvider", name => new InterfaceShadowBuilder(myModule, baseTypeInterfacesbuilders, name));
            var IAgreement = baseTypeInterfacesbuilders.GetOrAdd("Test.IAgreement", name => new InterfaceShadowBuilder(myModule, baseTypeInterfacesbuilders, name));


            IPaymentProvider.AddContraint("TType", typeof(DynamicEntity));
            IPaymentProvider.AddContraint("TType", IPaymentProviderType);

            IAgreement.AddContraint("TProvider", typeof(DynamicEntity));
            IAgreement.AddContraint("TProvider", IPaymentProvider);
            IAgreement.AddContraint("TType", typeof(DynamicEntity));
            IAgreement.AddContraint("TType", IPaymentProviderType);
           

            var IPaymentProviderTypeCompiledType = IPaymentProviderType.CreateType();
            var IPaymentProviderCompiledType = IPaymentProvider.CreateType();
            var IAgreementCompiledType = IAgreement.CreateType();

            var a = InterfaceShadowBuilder.DumpInterface(IPaymentProviderTypeCompiledType);
            var b = InterfaceShadowBuilder.DumpInterface(IPaymentProviderCompiledType);
            var c = InterfaceShadowBuilder.DumpInterface(IAgreementCompiledType);

            Assert.AreEqual(
              RemoveWhitespace("Test.IAgreement<TProvider,TType>\r\n      where TProvider : DynamicEntity, IPaymentProvider<TType>  \r\n where TType : DynamicEntity, IPaymentProviderType "),RemoveWhitespace( c));

            //Test.IAgreement<TProvider,TType>whereTType:DynamicEntity,IPaymentProviderTypewhereTProvider:DynamicEntity,IPaymentProvider<TType>>
            //Test.IAgreement<TProvider,TType>whereTProvider:DynamicEntity,IPaymentProvider<TType>whereTType:DynamicEntity,IPaymentProviderType>. 

        }
        public static string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        [TestMethod]
        [DeploymentItem(@"Specs/manifest.payments.json", "Specs")]
        [DeploymentItem(@"Specs/manifest.payments.sql", "Specs")]

        public async Task TestInherienceLevel2()
        {
            //Arrange
            var manifest = JToken.Parse(File.ReadAllText(@"Specs/manifest.payments.json")); 
            

            //Act
            var sql = RunDBWithSchema("payments", manifest);


            //Assure

            string expectedSQL = System.IO.File.ReadAllText(@"Specs/manifest.payments.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);

        }

        [TestMethod]
        [DeploymentItem(@"Specs/manifest.payments.json", "Specs")]
        public async Task TestInherienceLevel2Code()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.Schema = "dbo";               
                o.DTOBaseInterfaces = new[] {
                   typeof(IAgreement<,>), typeof(IPaymentProvider<>), typeof(IPaymentProviderType)
                };
            });

            var manifest = new ManifestService(new ManifestServiceOptions { MigrationName = "Latest", Namespace = "MC.Models", });

            var tables = manifest.BuildDynamicModel(codeMigratorV2, JToken.Parse(File.ReadAllText("Specs/manifest.payments.json")));
            var code = codeMigratorV2.GenerateCodeFiles();

        }
    }
}
