using EAVFramework.Shared;
using EAVFramework.Shared.V2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static EAVFramework.Shared.TypeHelper;

namespace EAVFramework.UnitTest
{
  
    [TestClass]
    public class EmitTests
    {
        public static string RemoveWhitespace( string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        [TestMethod]
        public void TestSimplePropertiesWithCircle()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions();

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models");

            var identity = assembly.WithTable("Identity", "Identity", "identity", "Identities", "dbo", true);
            var server = assembly.WithTable("Server", "Server", "server", "Servers", "dbo");


            identity.AddProperty("Id", "Id", "id", "guid");
            identity.AddProperty("Primary Server", "PrimaryServer", "primaryserver", server.GetTypeInfo());

            server.AddProperty("Id", "Id", "id", "guid");
            server.AddProperty("Created By", "CreatedBy", "createdby", identity.GetTypeInfo());

            server.BuildType();
            identity.BuildType();


            var identitType = identity.CreateTypeInfo();
            var serverType = server.CreateTypeInfo();

            Assert.AreEqual("Identity", identitType.Name);
            Assert.AreEqual("Server", serverType.Name);

            var code = codeMigratorV2.GenerateCodeFiles();

            AssertFiles(code, nameof(TestSimplePropertiesWithCircle));

        }

        [TestMethod]
        public void TestLookupsWithCircle()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions();

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models");

            var identity = assembly.WithTable("Identity", "Identity", "identity", "Identities", "dbo", true);
            var server = assembly.WithTable("Server", "Server", "server", "Servers", "dbo");


            identity.AddProperty("Id", "Id", "id", "guid");
            identity.AddProperty("Primary Server", "PrimaryServerId", "primaryserverid", "guid").LookupTo(server);


            server.AddProperty("Id", "Id", "id", "guid");
            server.AddProperty("Created By", "CreatedById", "createdbyid", "guid").LookupTo(identity);

            server.BuildType();
            identity.BuildType();

            var identitType = identity.CreateTypeInfo();
            var serverType = server.CreateTypeInfo();

            Assert.AreEqual("Identity", identitType.Name);
            Assert.AreEqual("Server", serverType.Name);

            var code = codeMigratorV2.GenerateCodeFiles();
           
            AssertFiles(code, nameof(TestLookupsWithCircle));

        }

        private static void AssertFiles(IDictionary<string, string> code, string folder)
        {
            foreach (var file in code)
            {
                Assert.AreEqual(RemoveWhitespace(File.ReadAllText("specs/" + folder + "/" + file.Key + ".txt")), RemoveWhitespace(file.Value), file.Key);
            }
        }

        [TestMethod]
        public void TestInherience()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOInterfaces = new[] { typeof(IHaveName), typeof(IIdentity) };
            });

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models");

            var identity = assembly.WithTable("Identity","Identity", "identity", "Identities", "dbo",true);
            var securityGroup = assembly.WithTable("SecurityGroup","SecurityGroup", "securitygroup", "SecurityGroups", "dbo");


            securityGroup.WithBaseEntity(identity);

            identity.AddProperty("Id","Id", "id", "guid");
            identity.AddProperty("Name", "Name", "name", "guid");

            securityGroup.AddProperty("Id", "Id", "id", "guid");

            securityGroup.BuildType();
            identity.BuildType();

            var securityGroupType = securityGroup.CreateTypeInfo();
            var identitType = identity.CreateTypeInfo();


            Assert.AreEqual("Identity", identitType.Name);
            Assert.AreEqual("SecurityGroup", securityGroupType.Name);

            var code = codeMigratorV2.GenerateCodeFiles();
            AssertFiles(code, nameof(TestInherience));
       
        }
        [TestMethod]
        public void TestBigCircleReference()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOInterfaces = new[] {  typeof(IOpenIdConnectIdentityResource),
                    typeof(IOpenIdConnectScope<>),
                    typeof(IOpenIdConnectResource<>),

                    typeof(IOpenIdConnectScopeResource<,>)
                };
            });
                 

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models");

            var OpenIdConnectIdentityResource = assembly.WithTable("OpenId Connect Identity Resource", "OpenIdConnectIdentityResource", "openidconnectidentityresource", "OpenIdConnectIdentityResources", "dbo", false);
            var OpenIdConnectScope = assembly.WithTable("OpenId Connect Scope", "OpenIdConnectScope", "openidconnectscope", "OpenIdConnectScopes", "dbo", false);
            var OpenIdConnectResource = assembly.WithTable("OpenId Connect Resource", "OpenIdConnectResource", "openidconnectresource", "OpenIdConnectResources","dbo",true);
            var OpenIdConnectScopeResource = assembly.WithTable("OpenId Connect Scope Resource", "OpenIdConnectScopeResource", "openidconnectscoperesource", "OpenIdConnectScopeResources");


            OpenIdConnectIdentityResource.WithBaseEntity(OpenIdConnectResource);
            OpenIdConnectScope.WithBaseEntity(OpenIdConnectResource);

            OpenIdConnectResource.BuildType();
            OpenIdConnectIdentityResource.BuildType();
            OpenIdConnectScope.BuildType();
            OpenIdConnectScopeResource.BuildType();

            var OpenIdConnectIdentityResourceType = OpenIdConnectIdentityResource.CreateTypeInfo();
            var OpenIdConnectResourceType = OpenIdConnectResource.CreateTypeInfo();
            var OpenIdConnectScopeResourceType = OpenIdConnectScopeResource.CreateTypeInfo();
           
           
            var OpenIdConnectScopeType = OpenIdConnectScope.CreateTypeInfo();
            
            


            Assert.AreEqual("OpenIdConnectIdentityResource", OpenIdConnectIdentityResourceType.Name);
            

            var code = codeMigratorV2.GenerateCodeFiles();
            AssertFiles(code, nameof(TestBigCircleReference));

        }
        [TestMethod]
        public void TestRaw()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOInterfaces = new[] { typeof(IHaveName), typeof(IIdentity) };
                o.DTOBaseClasses = new[] { typeof(BaseIdEntity<>), typeof(BaseOwnerEntity<>) };

            });

            var module = codeMigratorV2.CreateAssemblyBuilder("MC.Models");

                var moduleBuilder = module.Module;

                var classA = moduleBuilder.DefineType("TypeA", TypeAttributes.Public
                                                                            | TypeAttributes.Class
                                                                            | TypeAttributes.AutoClass
                                                                            | TypeAttributes.AnsiClass
                                                                            | TypeAttributes.Serializable
                                                                            | TypeAttributes.BeforeFieldInit);
                var classB = moduleBuilder.DefineType("TypeB", TypeAttributes.Public
                                                                            | TypeAttributes.Class
                                                                            | TypeAttributes.AutoClass
                                                                            | TypeAttributes.AnsiClass
                                                                            | TypeAttributes.Serializable
                                                                            | TypeAttributes.BeforeFieldInit);
                classA.AddInterfaceImplementation(typeof(IReference<,>).MakeGenericType(classA,classB));
                classB.AddInterfaceImplementation(typeof(IReference<,>).MakeGenericType(classB, classA));
                var classAType = classA.CreateType();
                var classBType = classB.CreateType();
        }

        [TestMethod]
        public void TestExternalBaseClass()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOInterfaces = new[] { typeof(IHaveName), typeof(IIdentity) };
                o.DTOBaseClasses = new[] { typeof(BaseIdEntity<>), typeof(BaseOwnerEntity<>) }; 
                
            });

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models");

            var identity = assembly.WithTable("Identity", "Identity", "identity", "Identities", "dbo", true);
            var securityGroup = assembly.WithTable("SecurityGroup", "SecurityGroup", "securitygroup", "SecurityGroups", "dbo");
             
            securityGroup.WithBaseEntity(identity);

            identity.AddProperty("Id", "Id", "id", "guid");
            identity.AddProperty("Name", "Name", "name", "text");

            identity.AddProperty("Awesome User", "AwesomeUserId", "awesomeuserid", "guid").LookupTo(identity);
             
            securityGroup.AddProperty("Id", "Id", "id", "guid");



            securityGroup.BuildType();
            identity.BuildType();

            var securityGroupType = securityGroup.CreateTypeInfo();
            var identitType = identity.CreateTypeInfo();


            Assert.AreEqual("Identity", identitType.Name);
            Assert.AreEqual("SecurityGroup", securityGroupType.Name);

            var code = codeMigratorV2.GenerateCodeFiles();
            AssertFiles(code, nameof(TestExternalBaseClass));
           

        }
        private static void NoOp(CodeGenerationOptions o) { }
        private static DynamicCodeService CreateOptions(Action<CodeGenerationOptions> onconfig =null)
        {
            onconfig ??= NoOp;
            var o = new CodeGenerationOptions
            {
              //  MigrationName="Initial",

                JsonPropertyAttributeCtor = typeof(JsonPropertyAttribute).GetConstructor(new Type[] { typeof(string) }),
                JsonPropertyNameAttributeCtor = typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).GetConstructor(new Type[] { typeof(string) }),
                InverseAttributeCtor = typeof(InversePropertyAttribute).GetConstructor(new Type[] { typeof(string) }),
                ForeignKeyAttributeCtor=  typeof(ForeignKeyAttribute).GetConstructor(new Type[] { typeof(string) }),
               
                EntityConfigurationInterface = typeof(IEntityTypeConfiguration),
                EntityConfigurationConfigureName = nameof(IEntityTypeConfiguration.Configure),
                EntityTypeBuilderType = typeof(EntityTypeBuilder),
                EntityTypeBuilderToTable = Resolve(()=> typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), 0, new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }), "EntityTypeBuilderToTable"),
                EntityTypeBuilderHasKey = Resolve(() => typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), 0, new[] { typeof(string[]) }), "EntityTypeBuilderHasKey"),
                EntityTypeBuilderPropertyMethod = Resolve(() => typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), 0, new[] { typeof(string) }), "EntityTypeBuilderPropertyMethod"),

                IsRequiredMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                   .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRequired)), "IsRequiredMethod"),
                IsRowVersionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                     .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRowVersion)), "IsRowVersionMethod"),
                HasConversionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                              .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasConversion), 1, new Type[] { }), "HasConversionMethod"),
                HasPrecisionMethod = Resolve(() => typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                                   .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasPrecision), new Type[] { typeof(int), typeof(int) }), "HasPrecisionMethod"),



                DynamicTableType = typeof(IDynamicTable),
                DynamicTableArrayType = typeof(IDynamicTable[]),


                ColumnsBuilderType = typeof(ColumnsBuilder),
                CreateTableBuilderType = typeof(CreateTableBuilder<>),
                CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),
                ColumnsBuilderColumnMethod = Resolve(() => typeof(ColumnsBuilder).GetMethod(nameof(ColumnsBuilder.Column), BindingFlags.Public | BindingFlags.Instance), "ColumnsBuilderColumnMethod"),
                OperationBuilderAddColumnOptionType = typeof(OperationBuilder<AddColumnOperation>),


                MigrationBuilderDropTable = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable)), "MigrationBuilderDropTable"),
                MigrationBuilderCreateTable = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)), "MigrationBuilderCreateTable"),
                MigrationBuilderSQL = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.Sql)), "MigrationBuilderSQL"),
                MigrationBuilderCreateIndex = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex), new Type[] { typeof(string), typeof(string), typeof(string[]), typeof(string), typeof(bool), typeof(string) }), "MigrationBuilderCreateIndex"),
                MigrationBuilderDropIndex = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropIndex)), "MigrationBuilderDropIndex"),
                MigrationsBuilderAddColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddColumn)), "MigrationsBuilderAddColumn"),
                MigrationsBuilderAddForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddForeignKey), new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(ReferentialAction), typeof(ReferentialAction) }), "MigrationsBuilderAddForeignKey"),
                MigrationsBuilderAlterColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AlterColumn)), "MigrationsBuilderAlterColumn"),
                MigrationsBuilderDropForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropForeignKey)), "MigrationsBuilderDropForeignKey"),

                ReferentialActionType = typeof(ReferentialAction),
                ReferentialActionNoAction = (int)ReferentialAction.NoAction,


                LambdaBase = Resolve(() => typeof(Expression).GetMethod(nameof(Expression.Lambda), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null), "LambdaBase"),

            };
            onconfig(o);
            return new DynamicCodeService(o);
        }
    }

    [EntityInterface(EntityKey ="*")]
    public interface IHaveName
    {
        public string Name { get; set; }
    }
    [EntityInterface(EntityKey = "Identity")]
    public interface IIdentity
    {

    }


    [BaseEntity]
    [Serializable]
    [GenericTypeArgument(ArgumentName = "TIdentity", ManifestKey = "Identity")]
    public class BaseIdEntity<TIdentity> : DynamicEntity where TIdentity : DynamicEntity
    {

        [DataMember(Name = "id")]
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [DataMember(Name = "awesomeuserid")]
        [JsonProperty("awesomeuserid")]
        [JsonPropertyName("awesomeuserid")]
        public Guid? AwesomeUserId { get; set; }

        [ForeignKey("AwesomeUserId")]
        [JsonProperty("awesomeuser")]
        [JsonPropertyName("awesomeuser")]
        [DataMember(Name = "awesomeuser")]
        public TIdentity AwesomeUser { get; set; }

    }

    [BaseEntity]
    [Serializable]
    [GenericTypeArgument(ArgumentName = "TIdentity", ManifestKey = "Identity")]
    public class BaseOwnerEntity<TIdentity> : BaseIdEntity<TIdentity> where TIdentity : DynamicEntity
    {
        [DataMember(Name = "ownerid")]
        [JsonProperty("ownerid")]
        [JsonPropertyName("ownerid")]
        public Guid? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        [DataMember(Name = "owner")]
        [JsonProperty("owner")]
        [JsonPropertyName("owner")]
        public TIdentity Owner { get; set; }
    }

    [EntityInterface(EntityKey = "OpenId Connect Identity Resource")]
    public interface IOpenIdConnectIdentityResource

    {
        
    }

    [EntityInterface(EntityKey = "OpenId Connect Scope Resource")]
    public interface IOpenIdConnectScopeResource<TOpenIdConnectResource, TOpenIdConnectIdentityResource>
        where TOpenIdConnectResource : DynamicEntity
        where TOpenIdConnectIdentityResource : DynamicEntity

    {

       // public TOpenIdConnectResource Resource { get; set; }
      //  public TOpenIdConnectIdentityResource Scope { get; set; }
    }


    [EntityInterface(EntityKey = "OpenId Connect Resource")]
    public interface IOpenIdConnectResource<TOpenIdConnectScopeResource>
        where TOpenIdConnectScopeResource:DynamicEntity
    {

         
      //  public ICollection<TOpenIdConnectScopeResource> OpenIdConnectScopeResources { get; set; }
    }


    [EntityInterface(EntityKey = "OpenId Connect Scope")]
    public interface IOpenIdConnectScope<TOpenIdConnectScopeResource>
        where TOpenIdConnectScopeResource : DynamicEntity
    {
        
       // public ICollection<TOpenIdConnectScopeResource> OpenIdConnectScopeResources { get; set; }
    }
    public interface IReference<T1,T2>
    {

    }
    public class ARef : IReference<ARef,BRef>
    {

    }
    public class BRef : IReference<BRef,ARef>
    {

    }
    public class OIDCScope : DynamicEntity,
        IOpenIdConnectScope<OIDCScopeResource>
    {

    }
    public class OIDCScopeResource : OIDCIdentityResource,
        IOpenIdConnectScopeResource<OIDCResource,OIDCIdentityResource>
    {
        public OIDCResource Resource { get; set; }
        public OIDCIdentityResource Scope { get; set; }
    }
    public class OIDCIdentityResource : DynamicEntity, IOpenIdConnectIdentityResource
    {

    }
    public class OIDCResource : DynamicEntity ,
        IOpenIdConnectResource<OIDCScopeResource>
    {
        public ICollection<OIDCScopeResource> OpenIdConnectScopeResources { get; set; }
    }
}
