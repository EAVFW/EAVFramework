using EAVFramework.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static EAVFramework.Shared.TypeHelper;

namespace EAVFramework.Shared.V2
{
    public interface IColumnPropertyResolver
    {
        object GetValue(string argName);
    }
    public class ManifestColumnMigrationColumnResolver : IColumnPropertyResolver
    {
        private JToken value;

        public ManifestColumnMigrationColumnResolver(JToken value)
        {
            this.value = value;
        }

        public object GetValue(string argName)
        {
            var a = value.SelectToken($"$.type.sql.{argName}");

            switch (a.Type)
            {
                case JTokenType.String:
                    return a.ToObject<string>();
                case JTokenType.Integer:
                    return a.ToObject<int>();
                case JTokenType.Float:
                    return a.ToObject<double>();
                   
            }
            return null;
        }
    }
    public class ManifestService
    {
     

        public ManifestService( )
        {
           
        }
        internal Type CreateDynamicMigration(DynamicCodeService dynamicCodeService, string @namespace, string migrationName, JToken manifest)
        {
            var asmb = dynamicCodeService.CreateAssemblyBuilder(@namespace);


            TypeBuilder migrationType =
                                         asmb.Module.DefineType($"{@namespace}.Migration{migrationName}", TypeAttributes.Public, typeof(DynamicMigration));

            ;

            var attributeBuilder = new CustomAttributeBuilder(Resolve(() => typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) }), "MigrationAttributeCtor"), new object[] { migrationName });
            migrationType.SetCustomAttribute(attributeBuilder);



            ConstructorBuilder entityTypeCtorBuilder =
                 migrationType.DefineConstructor(MethodAttributes.Public,
                                    CallingConventions.Standard, new[] { typeof(JToken), typeof(IDynamicTable[]) });

            var entityTypeCtorBuilderIL = entityTypeCtorBuilder.GetILGenerator();
            var basector = typeof(DynamicMigration).GetConstructor(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance, null, new[] { typeof(JToken), typeof(IDynamicTable[]) }, null);

            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_1);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_2);
            entityTypeCtorBuilderIL.Emit(OpCodes.Call, basector);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ret);


            //Assembly = builder;

            var type = migrationType.CreateTypeInfo();
            return type;
        }
        public (Type, IDynamicTable[]) BuildDynamicModel(DynamicCodeService dynamicCodeService, string @namespace,string migrationName, JToken manifest)
        {
            var builder = dynamicCodeService.CreateAssemblyBuilder(@namespace);
            var options = dynamicCodeService.Options;

            var tables = new Dictionary<string, DynamicTableBuilder>();
            foreach (var entity in manifest.SelectToken("$.entities").OfType<JProperty>())
            {
                var table = builder.WithTable(entity.Name,
                    tableSchemaname: entity.Value.SelectToken("$.schemaName").ToString(),
                    tableLogicalName: entity.Value.SelectToken("$.logicalName").ToString(),
                    tableCollectionSchemaName: entity.Value.SelectToken("$.collectionSchemaName").ToString(),
                     entity.Value.SelectToken("$.schema")?.ToString() ?? options.Schema,
                     entity.Value.SelectToken("$.abstract") != null
                    );

                tables.Add(entity.Name, table);
            }

            foreach (var entityDefinition in manifest.SelectToken("$.entities").OfType<JProperty>())
            {

                var table = tables[entityDefinition.Name];

                var parentName = entityDefinition.Value.SelectToken("$.TPT")?.ToString();
                if (!string.IsNullOrEmpty(parentName))
                {
                    table.WithBaseEntity(tables[parentName]);

                }


                var keys = entityDefinition.Value.SelectToken("$.keys") as JObject;
                if (keys != null)
                {
                    foreach (var key in keys.OfType<JProperty>())
                    {
                        var props = key.Value.ToObject<string[]>();

                        table.AddKeys(key.Name,props);
                    }
                }


                        foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
                {
                    var typeObj = attributeDefinition.Value.SelectToken("$.type");
                    var type = attributeDefinition.Value.SelectToken("$.type.type")?.ToString();
                    var isprimaryKey = attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;
                    var attributeKey = attributeDefinition.Name;
                    var schemaName = attributeDefinition.Value.SelectToken("$.schemaName").ToString();
                    var logicalName = attributeDefinition.Value.SelectToken("$.logicalName").ToString();

                    var propertyInfo = table
                        .AddProperty(attributeKey, schemaName, logicalName, type)
                        .WithExternalHash(HashExtensions.Sha256(attributeDefinition.Value.ToString()))
                        .WithExternalTypeHash(HashExtensions.Sha256(attributeDefinition.Value.SelectToken("$.type")?.ToString()))
                        .WithDescription(attributeDefinition.Value.SelectToken("$.type.description")?.ToString() ?? attributeDefinition.Value.SelectToken("$.description")?.ToString() )
                        .WithMigrationColumnProvider(new ManifestColumnMigrationColumnResolver(attributeDefinition.Value) as IColumnPropertyResolver)
                        .WithMaxLength((typeObj?.SelectToken("$.sql.maxLength") ?? typeObj?.SelectToken("$.maxLength"))?.ToObject<int>())
                        .Required(attributeDefinition.Value.SelectToken("$.type.required")?.ToObject<bool>() ?? attributeDefinition.Value.SelectToken("$.required")?.ToObject<bool>()??false)
                        .RowVersion((attributeDefinition.Value.SelectToken("$.isRowVersion")?.ToObject<bool>() ?? false));
                   
                                
                    if (isprimaryKey)
                    {

                        propertyInfo.PrimaryKey();
                        
                    }

                    if(type == "lookup")
                    {
                        propertyInfo
                            .LookupTo(
                                tables[attributeDefinition.Value.SelectToken("$.type.referenceType")?.ToString()],
                                //attributeDefinition.Value.SelectToken("$.type.foreignKey")?.ToObject<ForeignKeyInfo>(),
                                attributeDefinition.Value.SelectToken("$.type.cascade.delete")?.ToObject(options.ReferentialActionType),
                                attributeDefinition.Value.SelectToken("$.type.cascade.update")?.ToObject(options.ReferentialActionType))
                            .WithIndex(attributeDefinition.Value.SelectToken("$.type.index") != null ?
                                attributeDefinition.Value.SelectToken("$.type.index")?.ToObject<IndexInfo>() ?? new IndexInfo { Unique = true }:null);

                   
                       
                    }


                   

                }
            }

            foreach (var entity in manifest.SelectToken("$.entities").OfType<JProperty>())
            {

                var table = tables[entity.Name];
                table.BuildType();
            }

            foreach (var entity in manifest.SelectToken("$.entities").OfType<JProperty>())
            {

                var table = tables[entity.Name];
                table.CreateConfigurationTypeInfo();
                table.CreateTypeInfo();
            }



            return (CreateDynamicMigration(dynamicCodeService, @namespace, migrationName, manifest),
                tables.Values.Select(entity => entity.CreateMigrationType(migrationName)).Select(entity => Activator.CreateInstance(entity) as IDynamicTable).ToArray());



        }
    }
}
