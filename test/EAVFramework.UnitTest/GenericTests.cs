using EAVFramework.Configuration;
using EAVFramework.Endpoints;
using EAVFramework.Shared;
using EAVFramework.Shared.V2;
using EAVFramework.UnitTest.ManifestTests;
using EAVFW.Extensions.Manifest.SDK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public interface IAgreement<TProvider, TType>
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
            var IPaymentProviderType = baseTypeInterfacesbuilders.GetOrAdd("Test.IPaymentProviderType", name => new InterfaceShadowBuilder(myModule, baseTypeInterfacesbuilders, name));
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
              RemoveWhitespace("Test.IAgreement<TProvider,TType>\r\n      where TProvider : DynamicEntity, IPaymentProvider<TType>  \r\n where TType : DynamicEntity, IPaymentProviderType "), RemoveWhitespace(c));

            //Test.IAgreement<TProvider,TType>whereTType:DynamicEntity,IPaymentProviderTypewhereTProvider:DynamicEntity,IPaymentProvider<TType>>
            //Test.IAgreement<TProvider,TType>whereTProvider:DynamicEntity,IPaymentProvider<TType>whereTType:DynamicEntity,IPaymentProviderType>. 

        }
        public static string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        public static void TestType<T>() where T : DynamicEntity, ISigninRecord, new()
        {
            var test = new T { };
            var p = test.Properties;
            test.Provider = "da";
            //var a = test.Test;
            var t = test.Id;
            test.Id = Guid.NewGuid();
        }

        [TestMethod]
        [DeploymentItem(@"Specs/mc-crm.json", "Specs")]
        [DeploymentItem(@"Specs/mc-crm.sql", "Specs")]

        public async Task TestCRMModule()
        {
            //Arrange
            var manifest = JToken.Parse(File.ReadAllText(@"Specs/mc-crm.json"));


            //Act
            var (sql, sp) = RunDBWithSchema("cmr", o =>
            {

                o.DTOBaseInterfaces = new Type[] {
                    typeof(IHaveName),
                    typeof(IIdentity),
                     typeof(ISigninRecord)
                };
                o.DTOBaseClasses = new[] { typeof(FullBaseIdEntity<>), typeof(FullBaseOwnerEntity<>) };
            }, manifest);

            var ctx = sp.GetService<DynamicContext>();
            var t = ctx.GetEntityType("Signins");
            this.GetType().GetMethod("TestType").MakeGenericMethod(t).Invoke(this, null);

            string expectedSQL = System.IO.File.ReadAllText(@"Specs/mc-crm.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);

        }


        [TestMethod]
        [DeploymentItem(@"Specs/mc-padel.json", "Specs")]
        [DeploymentItem(@"Specs/mc-padel.sql", "Specs")]

        public async Task TestPadelModule()
        {
            //Arrange
            var manifest = JToken.Parse(File.ReadAllText(@"Specs/mc-padel.json"));


            //Act
            var (sql, sp) = RunDBWithSchema("padel", o =>
            {

                o.DTOBaseInterfaces = new Type[] {
                    typeof(IIdentity),  typeof(IIdentity),
                  typeof(IContact),typeof(IWebsite),typeof(IContactInformation<>)

                };
                o.DTOBaseClasses = new[] { typeof(FullBaseIdEntity<>), typeof(FullBaseOwnerEntity<>) };
            }, manifest);

            var ctx = sp.GetService<DynamicContext>();


            string expectedSQL = System.IO.File.ReadAllText(@"Specs/mc-padel.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);

        }

        [TestMethod]
        [DeploymentItem(@"Specs/mc-oauth.json", "Specs")]
        [DeploymentItem(@"Specs/mc-padel.sql", "Specs")]

        public async Task TestOAuthModule()
        {
            //Arrange
            var manifest = JToken.Parse(File.ReadAllText(@"Specs/mc-oauth.json"));


            //Act
            var (sql, sp) = RunDBWithSchema("oauth", o =>
            {

                o.DTOBaseInterfaces = new Type[] {
                    typeof(IIdentity),  typeof(IIdentity),
                  typeof(IContact),typeof(IWebsite),typeof(IContactInformation<>),
                     typeof(IOAuthContext<>),typeof(IAccount),
                    typeof(IOAuthContextWithDineroOrgs<>),typeof(IDineroOrg)

                };
                o.DTOBaseClasses = new[] { typeof(FullBaseIdEntity<>), typeof(FullBaseOwnerEntity<>) };
            }, manifest);

            var ctx = sp.GetService<DynamicContext>();


            
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOBaseInterfaces = new Type[] {
                    typeof(IAccount),
                      typeof(IOAuthContext<>)
                };
                o.DTOBaseClasses = new[] { typeof(FullBaseIdEntity<>), typeof(FullBaseOwnerEntity<>) };
                o.Schema = "oauth";
                o.GenerateAbstractClasses = false;


            });
            var manifestservice = new ManifestService(codeMigratorV2, new ManifestServiceOptions { MigrationName = "Latest", Namespace = "MC.Models", });
            
            var tables = manifestservice.BuildDynamicModel(codeMigratorV2, sp.GetService<IOptions<DynamicContextOptions>>().Value.Manifests.First());
            var code = codeMigratorV2.GenerateCodeFiles();

           

      //      string expectedSQL = System.IO.File.ReadAllText(@"Specs/mc-padel.sql");

        //    MigrationAssert.AreEqual(expectedSQL, sql);

        }

        [TestMethod]
        [DeploymentItem(@"Specs/mc-oidc.json", "Specs")]
        [DeploymentItem(@"Specs/mc-oidc.sql", "Specs")]

        public async Task TestOIDCModule()
        {
            //Arrange
            var manifest = JToken.Parse(File.ReadAllText(@"Specs/mc-oidc.json"));


            //Act
            var (sql, sp) = RunDBWithSchema("oidc", o =>
            {

            o.DTOBaseInterfaces = new Type[] {
                    typeof(IIdentity),  typeof(IIdentity),typeof(IAuditFields), //typeof(IAuditOwnerFields<>),
                  typeof(IContact),typeof(IWebsite),typeof(IContactInformation<>),
                   typeof(IOpenIdConnectClient<,>),
            typeof(IOpenIdConnectAuthorization<,,>),
            typeof(IOpenIdConnectToken<,,,>),
            typeof(IAllowedGrantType<>),
            typeof(IOpenIdConnectAuthorizationScope<>),
            typeof(IOpenIdConnectScopeResource<,>),
            typeof(IOpenIdConnectScope<>),
            typeof(IOpenIdConnectIdentityResource),
            typeof(IOpenIdConnectResource),
            typeof(IOpenIdConnectSecret),

        };
        o.DTOBaseClasses = new[] { typeof(FullBaseIdEntity<>), typeof(FullBaseOwnerEntity<>)
    };
}, manifest);

var ctx = sp.GetService<DynamicContext>();


string expectedSQL = System.IO.File.ReadAllText(@"Specs/mc-oidc.sql");

MigrationAssert.AreEqual(expectedSQL, sql);

        }

        [TestMethod]
[DeploymentItem(@"Specs/manifest.payments.json", "Specs")]
[DeploymentItem(@"Specs/manifest.payments.sql", "Specs")]

public async Task TestInherienceLevel2()
{
    //Arrange
    var manifest = JToken.Parse(File.ReadAllText(@"Specs/manifest.payments.json"));


    //Act
    var (sql, _) = RunDBWithSchema("payments", manifest);


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

    var manifest = new ManifestService(codeMigratorV2, new ManifestServiceOptions { MigrationName = "Latest", Namespace = "MC.Models", });

    var tables = manifest.BuildDynamicModel(codeMigratorV2, JToken.Parse(File.ReadAllText("Specs/manifest.payments.json")));
    var code = codeMigratorV2.GenerateCodeFiles();

}
    }



    [EntityInterface(EntityKey = "OAuth Context")]

    public interface IOAuthContextWithDineroOrgs<TDineroOrg>
        where TDineroOrg : DynamicEntity, IDineroOrg
    {

        ICollection<TDineroOrg> DineroOrganizations { get; set; }

    }
    [EntityInterface(EntityKey = "Dinero Organization")]
    public interface IDineroOrg
    //   where TAccount : DynamicEntity, IAccount
    //  where TReconciliationProvider : DynamicEntity, IOAuthContext<TAccount>
    {
        Guid Id { get; set; }
        string Name { get; set; }
        Guid? OAuthContextId { get; set; }
        string ExternalId { get; set; }
        bool? IsPro { get; set; }
    }

    [EntityInterface(EntityKey = "Contact")]
public interface IContact
{
    Guid Id { get; set; }
    string Name { get; set; }

}
[EntityInterface(EntityKey = "Website")]
public interface IWebsite
{
    Guid Id { get; set; }
    Guid? AccountId { get; set; }

    string Domain { get; set; }


}
    [EntityInterface(EntityKey = "Account")]
    public interface IAccount
    {
        Guid Id { get; set; }
        string Name { get; set; }

        string PrimaryCompanyCode { get; set; }


    }

    [EntityInterface(EntityKey = "*")]
    [ConstraintMapping(EntityKey = "Account", ConstraintName = nameof(TAccount))]
    public interface IOAuthContext<TAccount>
       where TAccount : DynamicEntity, IAccount
    {
        Guid Id { get; set; }
        string Name { get; set; }

        Guid? AccountId { get; set; }
        string ExternalId { get; set; }
        string AuthContext { get; set; }
        TAccount Account { get; set; }
    }


    public interface IAuditOwnerFields
    {
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        public Guid? CreatedById { get; set; }
        public Guid? OwnerId { get; set; }

        public Guid? ModifiedById { get; set; }
        public byte[] RowVersion { get; set; }
    }

    [EntityInterface(EntityKey = "*")]

    public interface IAuditOwnerFields<T> : IAuditOwnerFields
       where T : IIdentity
    {
        public T CreatedBy { get; set; }
        public T ModifiedBy { get; set; }

    }

    public interface IContactInformation
    {
        Guid Id { get; set; }
        public string Value { get; set; }
    }

    [EntityInterface(EntityKey = "Contact Information")]
[ConstraintMapping(AttributeKey = "Type", ConstraintName = nameof(TContactInformationType))]
public interface IContactInformation<TContactInformationType> : IContactInformation
   where TContactInformationType : struct, IConvertible
{


    

    TContactInformationType? Type { get; set; }
}

    [EntityInterface(EntityKey = "*")]
    public interface IAuditFields
    {
        public Guid? ModifiedById { get; set; }
        public Guid? CreatedById { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public byte[] RowVersion { get; set; }
    }

    [Serializable()]
    [EntityDTO(LogicalName = "oauthcontext", Schema = "oauth")]
    [Entity(LogicalName = "oauthcontext", SchemaName = "OAuthContext", CollectionSchemaName = "OAuthContexts", IsBaseClass = false, EntityKey = "OAuth Context")]
    public partial class OAuthContext : FullBaseOwnerEntity<Identity>, IOAuthContext<Account>
    {
        public OAuthContext()
        {
        }

        [DataMember(Name = "name")]
        [EntityField(AttributeKey = "Name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public String Name { get; set; }

        [DataMember(Name = "account")]
        [JsonProperty("account")]
        [JsonPropertyName("account")]
        [ForeignKey("AccountId")]
        public Account Account { get; set; }

        [DataMember(Name = "accountid")]
        [EntityField(AttributeKey = "Account")]
        [JsonProperty("accountid")]
        [JsonPropertyName("accountid")]
        public Guid? AccountId { get; set; }

        [DataMember(Name = "authcontext")]
        [EntityField(AttributeKey = "Auth Context")]
        [JsonProperty("authcontext")]
        [JsonPropertyName("authcontext")]
        public String AuthContext { get; set; }

        [DataMember(Name = "externalid")]
        [EntityField(AttributeKey = "External Id")]
        [JsonProperty("externalid")]
        [JsonPropertyName("externalid")]
        public String ExternalId { get; set; }

        [DataMember(Name = "dineroorganizations")]
        [JsonProperty("dineroorganizations")]
        [JsonPropertyName("dineroorganizations")]
        [InverseProperty("OAuthContext")]
        public ICollection<DineroOrganization> DineroOrganizations { get; set; }

    }
    [Serializable()]
    [EntityDTO(LogicalName = "account", Schema = "oauth")]
    [Entity(LogicalName = "account", SchemaName = "Account", CollectionSchemaName = "Accounts", IsBaseClass = false, EntityKey = "Account")]
    public partial class Account : FullBaseOwnerEntity<Identity>, IAccount
    {
        public Account()
        {
        }

        [DataMember(Name = "name")]
        [EntityField(AttributeKey = "Name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public String Name { get; set; }

          [DataMember(Name = "accounttypeid")]
        [EntityField(AttributeKey = "Account Type")]
        [JsonProperty("accounttypeid")]
        [JsonPropertyName("accounttypeid")]
        public Guid? AccountTypeId { get; set; }

        [DataMember(Name = "addressid")]
        [EntityField(AttributeKey = "Address")]
        [JsonProperty("addressid")]
        [JsonPropertyName("addressid")]
        public Guid? AddressId { get; set; }

   
        [DataMember(Name = "billingaddressid")]
        [EntityField(AttributeKey = "Billing Address")]
        [JsonProperty("billingaddressid")]
        [JsonPropertyName("billingaddressid")]
        public Guid? BillingAddressId { get; set; }

        [DataMember(Name = "eancode")]
        [EntityField(AttributeKey = "EAN Code")]
        [JsonProperty("eancode")]
        [JsonPropertyName("eancode")]
        public String EANCode { get; set; }

        [DataMember(Name = "externalid")]
        [EntityField(AttributeKey = "External Id")]
        [JsonProperty("externalid")]
        [JsonPropertyName("externalid")]
        public String ExternalId { get; set; }

        [DataMember(Name = "homepage")]
        [EntityField(AttributeKey = "Homepage")]
        [JsonProperty("homepage")]
        [JsonPropertyName("homepage")]
        public String Homepage { get; set; }

        [DataMember(Name = "primarycompanycode")]
        [EntityField(AttributeKey = "Primary Company Code")]
        [JsonProperty("primarycompanycode")]
        [JsonPropertyName("primarycompanycode")]
        public String PrimaryCompanyCode { get; set; }

 
        [DataMember(Name = "primaryemailid")]
        [EntityField(AttributeKey = "Primary Email")]
        [JsonProperty("primaryemailid")]
        [JsonPropertyName("primaryemailid")]
        public Guid? PrimaryEmailId { get; set; }

  
        [DataMember(Name = "primarylandlinephoneid")]
        [EntityField(AttributeKey = "Primary Landline Phone")]
        [JsonProperty("primarylandlinephoneid")]
        [JsonPropertyName("primarylandlinephoneid")]
        public Guid? PrimaryLandlinePhoneId { get; set; }

         [DataMember(Name = "primarymobilephoneid")]
        [EntityField(AttributeKey = "Primary Mobile Phone")]
        [JsonProperty("primarymobilephoneid")]
        [JsonPropertyName("primarymobilephoneid")]
        public Guid? PrimaryMobilePhoneId { get; set; }

   
        [DataMember(Name = "oauthcontexts")]
        [JsonProperty("oauthcontexts")]
        [JsonPropertyName("oauthcontexts")]
        [InverseProperty("Account")]
        public ICollection<OAuthContext> OAuthContexts { get; set; }

   
    }
    [Serializable()]
    [EntityDTO(LogicalName = "dineroorganization", Schema = "oauth")]
    [Entity(LogicalName = "dineroorganization", SchemaName = "DineroOrganization", CollectionSchemaName = "DineroOrganizations", IsBaseClass = false, EntityKey = "Dinero Organization")]
    public partial class DineroOrganization : FullBaseOwnerEntity<Identity>
    {
        public DineroOrganization()
        {
        }

        [DataMember(Name = "name")]
        [EntityField(AttributeKey = "Name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public String Name { get; set; }

        [DataMember(Name = "externalid")]
        [EntityField(AttributeKey = "External Id")]
        [JsonProperty("externalid")]
        [JsonPropertyName("externalid")]
        public String ExternalId { get; set; }

        [DataMember(Name = "ispro")]
        [EntityField(AttributeKey = "Is Pro")]
        [JsonProperty("ispro")]
        [JsonPropertyName("ispro")]
        public Boolean? IsPro { get; set; }

        [DataMember(Name = "oauthcontext")]
        [JsonProperty("oauthcontext")]
        [JsonPropertyName("oauthcontext")]
        [ForeignKey("OAuthContextId")]
        public OAuthContext OAuthContext { get; set; }

        [DataMember(Name = "oauthcontextid")]
        [EntityField(AttributeKey = "OAuth Context")]
        [JsonProperty("oauthcontextid")]
        [JsonPropertyName("oauthcontextid")]
        public Guid? OAuthContextId { get; set; }

    }
}
