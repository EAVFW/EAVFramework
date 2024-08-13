using EAVFramework.Shared;
using EAVFramework.Shared.V2;
using EAVFramework.UnitTest.ManifestTests;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static EAVFramework.Shared.TypeHelper;

namespace EAVFramework.UnitTest
{
    public class SuperSuper<T> : DynamicEntity
    {
        public Guid Id { get; set; }
        public Guid Test { get; set; }
    }

    public class Super : SuperSuper<TestIdentity>
    {
        
    }

    
    public class TestIdentity : DynamicEntity, IIdentity
    {
        public Guid Id { get; set; }
    }

    public interface ITest
    {
        Guid Id { get; set; }
    }

    [EntityInterface(EntityKey = "SecurityGroup")]
    public interface MyIdentity
    {
        Guid Id { get; set; }

    }

    [Serializable()]
    [EntityDTO(LogicalName = "identity", Schema = "dbo")]
    [Entity(LogicalName = "identity", SchemaName = "Identity", CollectionSchemaName = "Identities", IsBaseClass = true, EntityKey = "Identity")]
    public partial class Identity : BaseIdEntity<Identity>, IHaveName, IIdentity
    {
        public Identity()
        {
        }

        [DataMember(Name = "name")]
        [EntityField(AttributeKey = "Name")]
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        [PrimaryField()]
        public String Name { get; set; }

        [DataMember(Name = "identities")]
        [JsonProperty("identities")]
        [JsonPropertyName("identities")]
        [InverseProperty("AwesomeUser")]
        public ICollection<Identity> Identities { get; set; }

    }

    [TestClass]
    public class EmitTests : BaseManifestTests
    {
        public static string RemoveWhitespace( string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        [TestMethod]
        [DeploymentItem(@"Specs/TestSimplePropertiesWithCircle", "Specs/TestSimplePropertiesWithCircle")]
        public void TestSimplePropertiesWithCircle()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions();

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models","MC.Models");

            var identity = assembly.WithTable("Identity", "Identity", "identity", "Identities", "dbo", true);
            var server = assembly.WithTable("Server", "Server", "server", "Servers", "dbo");


            identity.AddProperty("Id", "Id", "id", "guid").PrimaryKey();
            identity.AddProperty("Primary Server", "PrimaryServer", "primaryserver", server.GetTypeInfo());

            server.AddProperty("Id", "Id", "id", "guid").PrimaryKey();
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
        [DeploymentItem(@"Specs/TestLookupsWithCircle", "Specs/TestLookupsWithCircle")]
        public void TestLookupsWithCircle()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions();

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models", "MC.Models");

            var identity = assembly.WithTable("Identity", "Identity", "identity", "Identities", "dbo", true);
            var server = assembly.WithTable("Server", "Server", "server", "Servers", "dbo");


            identity.AddProperty("Id", "Id", "id", "guid").PrimaryKey();
            identity.AddProperty("Primary Server", "PrimaryServerId", "primaryserverid", "guid").LookupTo(server);


            server.AddProperty("Id", "Id", "id", "guid").PrimaryKey();
            server.AddProperty("Created By", "CreatedById", "createdbyid", "guid").LookupTo(identity);

            server.BuildType();
            identity.BuildType();
            
            
            var serverType = server.CreateTypeInfo();
            var identitType = identity.CreateTypeInfo();
       

            Assert.AreEqual("Identity", identitType.Name);
            Assert.AreEqual("Server", serverType.Name);

            var code = codeMigratorV2.GenerateCodeFiles();
           
            AssertFiles(code, nameof(TestLookupsWithCircle));

        }

        private static void AssertFiles(IDictionary<string, string> code, string folder)
        {
            foreach (var file in code)
            {
                var a = File.ReadAllText("Specs/" + folder + "/" + file.Key + ".txt");
                var b = file.Value;
                if (RemoveWhitespace(a) != RemoveWhitespace(file.Value))
                {

                }
                Assert.AreEqual(
                    RemoveWhitespace(File.ReadAllText("Specs/" + folder + "/" + file.Key + ".txt")), RemoveWhitespace(file.Value), file.Key);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Specs/TestInherience", "Specs/TestInherience")]
        public void TestInherience()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOBaseInterfaces = new[] { typeof(IHaveName), typeof(IIdentity) };
            });

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models", "MC.Models");

            var identity = assembly.WithTable("Identity","Identity", "identity", "Identities", "dbo",true);
            var securityGroup = assembly.WithTable("SecurityGroup","SecurityGroup", "securitygroup", "SecurityGroups", "dbo");


            securityGroup.WithBaseEntity(identity);

            identity.AddProperty("Id","Id", "id", "guid").PrimaryKey();
            identity.AddProperty("Name", "Name", "name", "string").PrimaryField();

            securityGroup.AddProperty("Id", "Id", "id", "guid").PrimaryKey();

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
        [DeploymentItem(@"Specs/oidcclient.json", "Specs")]
        public void TestCoices()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.Schema = "dbo";
                o.DTOBaseClasses = new[] { typeof(FullBaseOwnerEntity<>), typeof(FullBaseIdEntity<>) };
                o.DTOBaseInterfaces = new[] {  typeof(IOpenIdConnectIdentityResource),
                    typeof(IOpenIdConnectScope<>),
                    typeof(IOpenIdConnectResource<>),
                    typeof(IOpenIdConnectClient<,>),
                    typeof(IAllowedGrantType<>),
                    typeof(IOpenIdConnectScopeResource<,>)
                };
            });

            var manifest = new ManifestService(codeMigratorV2,new ManifestServiceOptions { MigrationName = "Latest", Namespace = "MC.Models", });

            var tables = manifest.BuildDynamicModel(codeMigratorV2, JToken.Parse(File.ReadAllText("Specs/oidcclient.json")));
            var code = codeMigratorV2.GenerateCodeFiles();

            return;

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models", "MC.Models");
            var OpenIdConnectClient = assembly.WithTable("OpenId Connect Client", "OpenIdConnectClient", "openidconnectclient", "OpenIdConnectClients", "dbo", false);

            OpenIdConnectClient.AddProperty("Type", "Type", "type", "choice").AddChoiceOptions("ClientType",new Dictionary<string, int> { ["Test"] = 0, ["Public"] = 1 });
            OpenIdConnectClient.AddProperty("Consent Type", "ConsentType", "consenttype", "choice").AddChoiceOptions("ConsentType", new Dictionary<string, int> { ["Test"] = 0, ["Public"] = 1 });


            var AllowedGrantType = assembly.WithTable("Allowed Grant Type", "AllowedGrantType", "allowedgranttype", "AllowedGrantTypes", "dbo", false);
            AllowedGrantType.AddProperty("Allowed Grant Type Value", "AllowedGrantTypeValue", "allowedgranttypevalue", "choice").AddChoiceOptions("AllowedGrantTypeValues", new Dictionary<string, int> { ["Test"] = 0, ["Public"] = 1 });

            AllowedGrantType.AddProperty("OpenId Connect Client", "OpenIdConnectClientId", "openidconnectclientid", "guid").LookupTo(OpenIdConnectClient);

            OpenIdConnectClient.BuildType();
            AllowedGrantType.BuildType();
         
            var OpenIdConnectClientType = OpenIdConnectClient.CreateTypeInfo();

            var AllowedGrantTypeType = AllowedGrantType.CreateTypeInfo();




            var code1 = codeMigratorV2.GenerateCodeFiles();
        }
        [TestMethod]

        [DeploymentItem(@"Specs/TestBigCircleReference", "Specs/TestBigCircleReference")]
        public void TestBigCircleReference()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOBaseInterfaces = new[] {  
                    typeof(IOpenIdConnectIdentityResource),
                    typeof(IOpenIdConnectScope<>),
                    typeof(IOpenIdConnectResource<>),
                    
                    typeof(IOpenIdConnectScopeResource<,>)
                };
            });

          



            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models", "MC.Models");
         
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
            

            var code1 = codeMigratorV2.GenerateCodeFiles();
            AssertFiles(code1, nameof(TestBigCircleReference));

        }
        [Ignore]
        [TestMethod]
        public void TestRaw()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOBaseInterfaces = new[] { typeof(IHaveName), typeof(IIdentity) };
                o.DTOBaseClasses = new[] { typeof(BaseIdEntity<>), typeof(BaseOwnerEntity<>) };

            });

            var module = codeMigratorV2.CreateAssemblyBuilder("MC.Models", "MC.Models");

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

        public static void TestType<T>() where T : DynamicEntity, IIdentity, new()
        {
            var test = new T {  };
            var t = test.Id;
            test.Id = Guid.NewGuid();
        }

        public (System.Reflection.Emit.PropertyBuilder, FieldBuilder) CreateProperty(TypeBuilder builder, string name, Type type, PropertyAttributes props = PropertyAttributes.None,
           MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual)
        {
            try
            {
                // options.EntityDTOsBuilders
                var proertyBuilder = builder.DefineProperty(name, props, type, Type.EmptyTypes);

                FieldBuilder customerNameBldr = builder.DefineField($"_{name}",
                                                           type,
                                                           FieldAttributes.Private);

                // The property set and property get methods require a special
                // set of attributes.




                // Define the "get" accessor method for CustomerName.
                MethodBuilder custNameGetPropMthdBldr =
                    builder.DefineMethod($"get_{name}",
                                               methodAttributes,
                                               type,
                                               Type.EmptyTypes);

                ILGenerator custNameGetIL = custNameGetPropMthdBldr.GetILGenerator();

                custNameGetIL.Emit(OpCodes.Ldarg_0);
                custNameGetIL.Emit(OpCodes.Ldfld, customerNameBldr);
                custNameGetIL.Emit(OpCodes.Ret);

                // Define the "set" accessor method for CustomerName.
                MethodBuilder custNameSetPropMthdBldr =
                    builder.DefineMethod($"set_{name}",
                                               methodAttributes,
                                               null,
                                               new Type[] { type });

                ILGenerator custNameSetIL = custNameSetPropMthdBldr.GetILGenerator();

                custNameSetIL.Emit(OpCodes.Ldarg_0);
                custNameSetIL.Emit(OpCodes.Ldarg_1);
                custNameSetIL.Emit(OpCodes.Stfld, customerNameBldr);
                custNameSetIL.Emit(OpCodes.Ret);

                // Last, we must map the two methods created above to our PropertyBuilder to
                // their corresponding behaviors, "get" and "set" respectively.
                proertyBuilder.SetGetMethod(custNameGetPropMthdBldr);
                proertyBuilder.SetSetMethod(custNameSetPropMthdBldr);

                //foreach (var a in dynamicPropertyBuilder.TypeBuilder.GetInterfaces().Where(n => n.Name.Contains("IAuditOwnerFields")))
                //{

                //    try
                //    {
                //        var p = a.GetGenericTypeDefinition().GetProperties().FirstOrDefault(k => k.Name == name);
                //        if (p != null)
                //        {
                //            dynamicPropertyBuilder.TypeBuilder.DefineMethodOverride(custNameGetPropMthdBldr, p.GetMethod);
                //        }
                //    }
                //    catch (NotSupportedException)
                //    {

                //    }

                //}

                return (proertyBuilder, customerNameBldr);
            }
            catch (Exception ex)
            {

                throw new Exception($"Failed to create Property: {builder.Name}.{name}", ex);
            }
        }
        
        public class EmitBaseTestClass<T> : DynamicEntity where T:DynamicEntity
        {

            [DataMember(Name = "id")]
            [JsonProperty("id")]
            [JsonPropertyName("id")]
            public Guid Id { get; set; }


        }

        [TestMethod]
        public void TestSomething()
        {


            /*
             * 
             * We will generate 
             * 
             * class Identity : FullBaseIdEntity<Identity>, IHaveName, IIdentity
             * { 
             *     public string Name {get;set;}
             *     
             *     //System.TypeLoadException: Method 'get_Id' in type 'Identity' from assembly 'test, 
             *     //However this is a property on FullBaseIdEntity<>
             *     
             * }
             * 
             * and 
             * 
             * class Contact : Identity
             * {
             * 
             * }
             * 
             * 
             */
                       
            

            AssemblyName myAsmName = new AssemblyName("test");

            
            var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
              AssemblyBuilderAccess.RunAndCollect);
            

                        ModuleBuilder myModule =
              builder.DefineDynamicModule("test.dll");
            
            var identityBuilder = myModule.DefineType($"Identity", TypeAttributes.Public
                                                                        | TypeAttributes.Class
                                                                        | TypeAttributes.AutoClass
                                                                        | TypeAttributes.AnsiClass
                                                                        | TypeAttributes.Serializable
                                                                        | TypeAttributes.BeforeFieldInit);

            identityBuilder.SetParent(typeof(EmitBaseTestClass<>).MakeGenericType(identityBuilder));

            identityBuilder.AddInterfaceImplementation(typeof(IHaveName));
            identityBuilder.AddInterfaceImplementation(typeof(IIdentity));
            CreateProperty(identityBuilder, "Name", typeof(string));


            var propName = "Id";

            //var idInterfaceOverrider = identityBuilder.DefineProperty($"IIdentity.get_Id",
            //          PropertyAttributes.None,
            //          typeof(Guid),
            //          null);

            // var instane = typeof(FullBaseIdEntity<>).GetProperty(propName);

            var propert = identityBuilder.BaseType;
            var property = typeof(EmitBaseTestClass<DynamicEntity>).GetProperty(propName);
            {
                var base_get = identityBuilder.DefineMethod($"get_{propName}",
                    MethodAttributes.Virtual| MethodAttributes.Private | MethodAttributes.Final| MethodAttributes.SpecialName | MethodAttributes.NewSlot| MethodAttributes.HideBySig,
                    property.PropertyType, System.Type.EmptyTypes);
                var il = base_get.GetILGenerator();

               // il.DeclareLocal(typeof(Guid));

              //  il.Emit(OpCodes.Ldloca_S, 0);
              //  il.Emit(OpCodes.Initobj, typeof(Guid));
                il.Emit(OpCodes.Ldarg_0);
             //   il.EmitWriteLine("The value of 'x' is:");
              //  il.EmitWriteLine(xField);
                il.EmitCall(OpCodes.Call, property.GetGetMethod(), null);
                il.Emit(OpCodes.Ret);
                identityBuilder.DefineMethodOverride(base_get, typeof(IIdentity).GetProperty(propName).GetGetMethod());
             //   idInterfaceOverrider.SetGetMethod(base_get);
                //  

            }
            {
                var base_set = identityBuilder.DefineMethod($"set_{propName}", 
                    MethodAttributes.Virtual | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig,

                   null, new[] { property.PropertyType });
                var il = base_set.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, property.GetSetMethod());

                il.Emit(OpCodes.Ret);
                identityBuilder.DefineMethodOverride(base_set, typeof(IIdentity).GetProperty(propName).GetSetMethod());
             //   idInterfaceOverrider.SetSetMethod(base_set);

            }


            var contactBuilder = myModule.DefineType($"Contact", TypeAttributes.Public
                                                                        | TypeAttributes.Class
                                                                        | TypeAttributes.AutoClass
                                                                        | TypeAttributes.AnsiClass
                                                                        | TypeAttributes.Serializable
                                                                        | TypeAttributes.BeforeFieldInit);

            contactBuilder.SetParent(identityBuilder);
          



            var identityType = identityBuilder.CreateType();
            var contactType = contactBuilder.CreateType();
          

            this.GetType().GetMethod("TestType").MakeGenericMethod(contactType).Invoke(this, null);

             


        }

        [TestMethod]
        [DeploymentItem(@"Specs/TestExternalBaseClass", "Specs/TestExternalBaseClass")]
        public void TestExternalBaseClass()
        {
            DynamicCodeService codeMigratorV2 = CreateOptions(o =>
            {
                o.DTOBaseInterfaces = new Type[] { 
                    typeof(IHaveName), 
                    typeof(IIdentity) 
                };
                o.DTOBaseClasses = new[] { typeof(BaseIdEntity<>), typeof(BaseOwnerEntity<>) };
               
                o.GenerateAbstractClasses = false;
              
                
            });

            var assembly = codeMigratorV2.CreateAssemblyBuilder("MC.Models", "MC.Models");

            var identity = assembly.WithTable("Identity", "Identity", "identity", "Identities", "dbo", true);
            var securityGroup = assembly.WithTable("SecurityGroup", "SecurityGroup", "securitygroup", "SecurityGroups", "dbo");
            var contact = assembly.WithTable("Contact", "Contact", "contact", "Contracts", "dbo");

            securityGroup.WithBaseEntity(identity);
            contact.WithBaseEntity(identity);

            identity.AddProperty("Id", "Id", "id", "guid").PrimaryKey();
            identity.AddProperty("Name", "Name", "name", "text").PrimaryField();

           var a = identity.AddProperty("Awesome User", "AwesomeUserId", "awesomeuserid", "guid").LookupTo(identity);

            securityGroup.AddProperty("Id", "Id", "id", "guid");
            contact.AddProperty("Id", "Id", "id", "guid");


            //var typeBuilder = identity.Builder;

            //var superType = typeof(BaseIdEntity<>).MakeGenericType(identity.Builder);

            //MethodBuilder getterMethodBuilder = typeBuilder.DefineMethod("get_Id", MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(Guid), Type.EmptyTypes);
            //MethodBuilder setterMethodBuilder = typeBuilder.DefineMethod("set_Id", MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { typeof(Guid) });

            //// ILGenerator for getter
            //ILGenerator getterIL = getterMethodBuilder.GetILGenerator();
            //getterIL.Emit(OpCodes.Ldarg_0);
            //getterIL.Emit(OpCodes.Call, typeof(BaseIdEntity<>).GetProperty("Id").GetGetMethod());
            //getterIL.Emit(OpCodes.Ret);

            //// ILGenerator for setter
            //ILGenerator setterIL = setterMethodBuilder.GetILGenerator();
            //setterIL.Emit(OpCodes.Ldarg_0);
            //setterIL.Emit(OpCodes.Ldarg_1);
            //setterIL.Emit(OpCodes.Call, typeof(BaseIdEntity<>).GetProperty("Id").GetSetMethod());
            //setterIL.Emit(OpCodes.Ret);

            //typeBuilder.DefineMethodOverride(getterMethodBuilder, typeof(IIdentity).GetProperty("Id").GetGetMethod());
            //typeBuilder.DefineMethodOverride(setterMethodBuilder, typeof(IIdentity).GetProperty("Id").GetSetMethod());
            contact.BuildType();
            securityGroup.BuildType();
            identity.BuildType();


            // Map the methods to the property
            //   idPropertyBuilder.SetGetMethod(getterMethodBuilder);
            //   idPropertyBuilder.SetSetMethod(setterMethodBuilder);

            // Implement ITest.Id explicitly

           



            var securityGroupType = securityGroup.CreateTypeInfo();
            var identitType = identity.CreateTypeInfo();
            var contactType = contact.CreateTypeInfo();

            Assert.AreEqual("Identity", identitType.Name);
            Assert.AreEqual("SecurityGroup", securityGroupType.Name);

            this.GetType().GetMethod("TestType").MakeGenericMethod(identitType).Invoke(this, null);

            var code = codeMigratorV2.GenerateCodeFiles();
            AssertFiles(code, nameof(TestExternalBaseClass));
           

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
        public Guid Id { get; set; }
    }


    [BaseEntity]
    [Serializable]
    [GenericTypeArgument(ArgumentName = "TIdentity", ManifestKey = "Identity")]
    public class FullBaseIdEntity<TIdentity> : DynamicEntity where TIdentity : DynamicEntity
    {

        [DataMember(Name = "id")]
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [DataMember(Name = "modifiedbyid")]
        [JsonProperty("modifiedbyid")]
        [JsonPropertyName("modifiedbyid")]
        public Guid? ModifiedById { get; set; }

        [ForeignKey("ModifiedById")]
        [JsonProperty("modifiedby")]
        [JsonPropertyName("modifiedby")]
        [DataMember(Name = "modifiedby")]
        public TIdentity ModifiedBy { get; set; }

        [DataMember(Name = "createdbyid")]
        [JsonProperty("createdbyid")]
        [JsonPropertyName("createdbyid")]
        public Guid? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        [JsonProperty("createdby")]
        [JsonPropertyName("createdby")]
        [DataMember(Name = "createdby")]
        public TIdentity CreatedBy { get; set; }

        [DataMember(Name = "modifiedon")]
        [JsonProperty("modifiedon")]
        [JsonPropertyName("modifiedon")]
        public DateTime? ModifiedOn { get; set; }

        [DataMember(Name = "createdon")]
        [JsonProperty("createdon")]
        [JsonPropertyName("createdon")]
        public DateTime? CreatedOn { get; set; }

        [DataMember(Name = "rowversion")]
        [JsonProperty("rowversion")]
        [JsonPropertyName("rowversion")]
        public byte[] RowVersion { get; set; }

    }
    [BaseEntity]
    [Serializable]
    [GenericTypeArgument(ArgumentName = "TIdentity", ManifestKey = "Identity")]
    public class FullBaseOwnerEntity<TIdentity> : FullBaseIdEntity<TIdentity> where TIdentity : DynamicEntity
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

    [EntityInterface(EntityKey = "Allowed Grant Type")]
    [ConstraintMapping(AttributeKey = "Allowed Grant Type Value", ConstraintName = nameof(TAllowedGrantTypeValue))]
    public interface IAllowedGrantType<TAllowedGrantTypeValue>
        where TAllowedGrantTypeValue : struct, IConvertible
    {
        public TAllowedGrantTypeValue? AllowedGrantTypeValue { get; set; }
    }

    [EntityInterface(EntityKey = "OpenId Connect Client")]
    [ConstraintMapping(AttributeKey = "Consent Type", ConstraintName = "TOpenIdConnectClientConsentTypes")]
    [ConstraintMapping(AttributeKey = "Type", ConstraintName = "TOpenIdConnectClientTypes")]
   // [ConstraintMapping(EntityKey = "Allowed Grant Type", ConstraintName = nameof(TAllowedGrantType))]

    public interface IOpenIdConnectClient<TOpenIdConnectClientTypes, TOpenIdConnectClientConsentTypes>
        where TOpenIdConnectClientTypes : struct, IConvertible
        where TOpenIdConnectClientConsentTypes : struct, IConvertible
       // where TAllowedGrantType : DynamicEntity //, IAllowedGrantType<TAllowedGrantTypeValue>
    {
        public TOpenIdConnectClientTypes? Type { get; set; }

        public TOpenIdConnectClientConsentTypes? ConsentType { get; set; }

      //  public ICollection<TAllowedGrantType> AllowedGrantTypes { get; set; }
    }
       
    [EntityInterface(EntityKey = "OpenId Connect Identity Resource")]
    public interface IOpenIdConnectIdentityResource

    {
        
    }

    [EntityInterface(EntityKey = "OpenId Connect Scope Resource")]
    [ConstraintMapping(EntityKey = "OpenId Connect Resource", ConstraintName = nameof(TOpenIdConnectResource))]
    [ConstraintMapping(EntityKey = "OpenId Connect Identity Resource", ConstraintName = nameof(TOpenIdConnectIdentityResource))]
    public interface IOpenIdConnectScopeResource<TOpenIdConnectResource, TOpenIdConnectIdentityResource>
        where TOpenIdConnectResource : DynamicEntity
         where TOpenIdConnectIdentityResource : DynamicEntity

    {

        // public TOpenIdConnectResource Resource { get; set; }
        //  public TOpenIdConnectIdentityResource Scope { get; set; }
    }



    [EntityInterface(EntityKey = "OpenId Connect Resource")]
    public interface IOpenIdConnectResource<TOpenIdConnectScopeResource> where TOpenIdConnectScopeResource : DynamicEntity
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
