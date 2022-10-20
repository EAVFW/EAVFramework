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
            if (a == null)
                return null;


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
    public class ManifestServiceOptions
    {
        public string Namespace { get; set; }
        public string MigrationName { get; set; }

        public bool GenerateDTO { get; set; } = true;
        public bool PartOfMigration { get;  set; }

        public string ModuleName => $"{Namespace}_{MigrationName}";
    }
    public class ManifestService
    {
        private readonly ManifestServiceOptions options;
        private readonly IChoiceEnumBuilder choiceEnumBuilder;

        public ManifestService(ManifestServiceOptions options, IChoiceEnumBuilder choiceEnumBuilder = null)
        {
            this.options = options;
            this.choiceEnumBuilder = choiceEnumBuilder ?? new DefaultChoiceEnumBuilder();
        }
        internal Type CreateDynamicMigration(DynamicCodeService dynamicCodeService, JToken manifest)
        {
            var asmb = dynamicCodeService.CreateAssemblyBuilder(options.ModuleName , options.Namespace);


            TypeBuilder migrationType =
                                         asmb.Module.DefineType($"{options.Namespace}.Migration{this.options.MigrationName}", TypeAttributes.Public, typeof(DynamicMigration));

            ;

            var attributeBuilder = new CustomAttributeBuilder(Resolve(() => typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) }), "MigrationAttributeCtor"), new object[] { this.options.MigrationName });
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
        public (Type, IDynamicTable[]) BuildDynamicModel(DynamicCodeService dynamicCodeService,   JToken manifest)
        {
            var builder = dynamicCodeService.CreateAssemblyBuilder(this.options.ModuleName,this.options.Namespace);
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
                    ).External(entity.Value.SelectToken("$.external")?.ToObject<bool>()??false);

                tables.Add(entity.Name, table);
            }

          //  if (this.options.GenerateDTO)
            {

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

                            table.AddKeys(key.Name, props);
                        }
                    }



                    foreach (var attributeDefinition in entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>())
                    {
                        try
                        {
                            var typeObj = attributeDefinition.Value.SelectToken("$.type");
                            var type =( attributeDefinition.Value.SelectToken("$.type.type")?.ToString() ?? attributeDefinition.Value.SelectToken("$.type")?.ToString())?.ToLower();
                            var isprimaryKey = attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;
                            var isPrimaryField = attributeDefinition.Value.SelectToken("$.isPrimaryField")?.ToObject<bool>() ?? false;
                            var attributeKey = attributeDefinition.Name;
                            var schemaName = attributeDefinition.Value.SelectToken("$.schemaName").ToString();
                            var logicalName = attributeDefinition.Value.SelectToken("$.logicalName").ToString();

                            if (isprimaryKey && !string.IsNullOrEmpty(parentName))
                                continue;

                            if (type == "choices")
                                continue;

                            var propertyInfo = table
                                .AddProperty(attributeKey, schemaName, logicalName, type)
                                .WithExternalHash(HashExtensions.Sha256(attributeDefinition.Value.ToString()))
                                .WithExternalTypeHash(HashExtensions.Sha256(attributeDefinition.Value.SelectToken("$.type")?.ToString()))
                                .WithDescription(attributeDefinition.Value.SelectToken("$.type.description")?.ToString() ?? attributeDefinition.Value.SelectToken("$.description")?.ToString())
                                .WithMigrationColumnProvider(new ManifestColumnMigrationColumnResolver(attributeDefinition.Value))
                                .WithMaxLength((typeObj?.SelectToken("$.sql.maxLength") ?? typeObj?.SelectToken("$.maxLength"))?.ToObject<int>())
                                .Required(attributeDefinition.Value.SelectToken("$.type.required")?.ToObject<bool>() ?? attributeDefinition.Value.SelectToken("$.required")?.ToObject<bool>() ?? false)
                                .RowVersion((attributeDefinition.Value.SelectToken("$.isRowVersion")?.ToObject<bool>() ?? false));


                            if (isprimaryKey)
                            {

                                propertyInfo.PrimaryKey();

                            }

                            if (isPrimaryField)
                            {

                                propertyInfo.PrimaryField();

                            }

                            if (type == "lookup")
                            {
                                propertyInfo
                                    .LookupTo(
                                        tables[attributeDefinition.Value.SelectToken("$.type.referenceType")?.ToString()],
                                        //attributeDefinition.Value.SelectToken("$.type.foreignKey")?.ToObject<ForeignKeyInfo>(),
                                        attributeDefinition.Value.SelectToken("$.type.cascade.delete")?.ToObject(options.ReferentialActionType),
                                        attributeDefinition.Value.SelectToken("$.type.cascade.update")?.ToObject(options.ReferentialActionType))
                                    .WithIndex(attributeDefinition.Value.SelectToken("$.type.index") != null ?
                                        attributeDefinition.Value.SelectToken("$.type.index")?.ToObject<IndexInfo>() ?? new IndexInfo { Unique = true } : null);



                            }

                            if(type == "choice")
                            {
                                var choices = attributeDefinition.Value.SelectToken("$.type.options").OfType<JProperty>().ToDictionary(optionPro => choiceEnumBuilder.GetLiteralName(optionPro.Name), optionPro => (optionPro.Value.Type == JTokenType.Object ? optionPro.Value["value"] : optionPro.Value).ToObject<int>());
                                propertyInfo.AddChoiceOptions(choiceEnumBuilder.GetEnumName(attributeDefinition.Value,this.options.Namespace), choices);
                               // propertyInfo.prop
                            }



                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Failed to generate field {attributeDefinition.Name}", ex);
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

            }

            return (CreateDynamicMigration(dynamicCodeService, manifest),
                tables.Values.Select(entity => entity.CreateMigrationType(this.options.MigrationName, this.options.PartOfMigration)).Select(entity => Activator.CreateInstance(entity) as IDynamicTable).ToArray());



        }
    }
}
