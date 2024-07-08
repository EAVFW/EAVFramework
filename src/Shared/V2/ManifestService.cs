using EAVFramework.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using static EAVFramework.Shared.TypeHelper;

namespace EAVFramework.Shared.V2
{

    public class InterfaceShadowBuilderContraintContainer
    {
        public int Order { get; set; }
        public List<InterfaceShadowBuilderContraint> Constraints { get; } = new List<InterfaceShadowBuilderContraint>();
    }
    public class InterfaceShadowBuilderContraint
    {
        public Type Type { get; internal set; }
        public InterfaceShadowBuilder Reference { get; internal set; }
        public string[] TypeToEntityKeys { get; internal set; }
    }
    public class InterfaceShadowBuilder
    {
        public TypeBuilder Builder { get; }

        public InterfaceShadowBuilder(ModuleBuilder myModule, ConcurrentDictionary<string, InterfaceShadowBuilder> baseTypeInterfacesbuilders, string fullname)
        {
            this.Builder = myModule.DefineType(fullname, TypeAttributes.Public
                                                             | TypeAttributes.Interface
                                                             | TypeAttributes.Abstract
                                                             | TypeAttributes.AutoClass
                                                             | TypeAttributes.AnsiClass
                                                             | TypeAttributes.Serializable
                                                             | TypeAttributes.BeforeFieldInit);

            // AddEntityKeyAttributes(@interface as INamedTypeSymbol, interfaceEntityType);


        }



        public ConcurrentDictionary<string, InterfaceShadowBuilderContraintContainer> Contraints = new ConcurrentDictionary<string, InterfaceShadowBuilderContraintContainer>();

        public void AddContraint(string name, InterfaceShadowBuilder interfaceShadowBuilder)
        {
            var a = Contraints.GetOrAdd(name, new InterfaceShadowBuilderContraintContainer(){ Order = Contraints.Count });
            a.Constraints.Add(new InterfaceShadowBuilderContraint { Reference = interfaceShadowBuilder });
        }

        public void AddContraint(string name, Type type)
        {
            var a = Contraints.GetOrAdd(name, new InterfaceShadowBuilderContraintContainer() { Order = Contraints.Count });
            a.Constraints.Add(new InterfaceShadowBuilderContraint { Type = type });
        }

        public void BuildConstraints()
        {
            if (Contraints.Keys.Any())
            {

                var builders = Builder.DefineGenericParameters(Contraints.OrderBy(c=>c.Value.Order).Select(c=>c.Key).ToArray());
                foreach (var contraintbuilder in builders)
                {
                    var container = Contraints[contraintbuilder.Name];
                   
                    var types = Contraints[contraintbuilder.Name].Constraints.ToArray();

                    var contraints = types.Select(c => {

                        if (c.Type != null)
                            return c.Type;

                        var type = c.Reference.CreateType();

                        if (type.IsGenericType)
                        {

                          //  var ttype = builders.FirstOrDefault(n => n.Name == "TType");
                            var ttype = type.GetGenericArguments().Select(cc => builders.FirstOrDefault(n => n.Name == cc.Name)).ToArray();
                            return type.MakeGenericType(ttype);
                            throw new InvalidOperationException("contraint is generic " + type.Name);
                            // return type.MakeGenericType(c.TypeToEntityKeys.Select(k => builders.First(b => b.Name == k)).ToArray());
                        }

                        if (c.TypeToEntityKeys != null)
                        {

                        }

                        return type;


                    }
                    ).ToArray();

                    contraintbuilder.SetInterfaceConstraints(contraints);
                }
            }
        }
        public void Build()
        {
            if (IsBuilded)
                return;
            IsBuilded = true;

            BuildConstraints();
        }
        public bool IsBuilded { get; private set; }
        public Type CreateType()
        {
            Build();
            return Builder.CreateTypeInfo();
        }

        internal void AddContraint(string name, InterfaceShadowBuilder interfaceShadowBuilder, string[] typeToEntityKeys)
        {
            var a = Contraints.GetOrAdd(name, new InterfaceShadowBuilderContraintContainer() { Order = Contraints.Count });
            a.Constraints.Add(new InterfaceShadowBuilderContraint { Reference = interfaceShadowBuilder, TypeToEntityKeys = typeToEntityKeys });
        }

        public static string DumpInterface(Type value)
        {
            var sb = new StringBuilder();
            var constraints = new StringBuilder();
            sb.Append(value.FullName);
            if (value.ContainsGenericParameters)
            {
                sb.Append("<");
                var f = false;
                foreach (var pa in value.GetGenericArguments())
                {
                    if (f)
                        sb.Append(",");
                    f = true;
                    sb.Append(pa.Name);
                    var a = pa.GetGenericParameterConstraints();
                    if (a.Any())
                    {
                        var ger = string.Join(", ", a.Select(aa => {

                            if (aa.IsGenericType)
                                return $"{aa.Name}<{string.Join(",",aa.GetGenericArguments().Select(ga=>ga.Name))}>";

                            return aa.Name;
                        }));
                        constraints.AppendLine($"where {pa.Name} : {ger}");
                      
                        
                    }
                }
                sb.Append(">");

                sb.AppendLine();
                sb.AppendLine(constraints.ToString());


            }

            return sb.ToString();
        }

        internal void AddContraint(string name)
        {
            var a = Contraints.GetOrAdd(name, new InterfaceShadowBuilderContraintContainer() { Order = Contraints.Count });
        }
    }

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
        public bool PartOfMigration { get; set; }
        public string ModuleName => $"{Namespace}_{MigrationName}";

        public Dictionary<string, Type> EntityDTOs { get; set; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Type> EntityDTOConfigurations { get; set; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        public Assembly DTOAssembly { get; set; }
        public bool SkipValidateSchemaNameForRemoteTypes { get; internal set; }
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
            var asmb = dynamicCodeService.CreateAssemblyBuilder(options.ModuleName, options.Namespace);


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
        public (Type, IDynamicTable[]) BuildDynamicModel(DynamicCodeService dynamicCodeService, JToken manifest)
        {
            var builder = dynamicCodeService.CreateAssemblyBuilder(this.options.ModuleName, this.options.Namespace);
            var options = dynamicCodeService.Options;

            var tables = new Dictionary<string, DynamicTableBuilder>();
            foreach (var entityDefinition in manifest.SelectToken("$.entities").OfType<JProperty>())
            {
                
                var schema = entityDefinition.Value.SelectToken("$.schema")?.ToString() ?? options.Schema ?? "dbo";
                var result = this.options.DTOAssembly?.GetTypes().FirstOrDefault(t => t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute attr && attr.LogicalName == entityDefinition.Value.SelectToken("$.logicalName").ToString() && (this.options.SkipValidateSchemaNameForRemoteTypes || string.Equals(attr.Schema, schema, StringComparison.OrdinalIgnoreCase)))?.GetTypeInfo();


                var table = builder.WithTable(entity.Name,
                    tableSchemaname: entity.Value.SelectToken("$.schemaName").ToString(),
                    tableLogicalName: entity.Value.SelectToken("$.logicalName").ToString(),
                    tableCollectionSchemaName: entity.Value.SelectToken("$.collectionSchemaName").ToString(),
                     entity.Value.SelectToken("$.schema")?.ToString() ?? options.Schema,
                     entity.Value.SelectToken("$.abstract") != null,
                     entity.Value.SelectToken("$.mappingStrategy")?.ToObject<MappingStrategy>()
                    ).External(entity.Value.SelectToken("$.external")?.ToObject<bool>() ?? false, result);

                var upSqlToken = entityDefinition.Value.SelectToken("$.sql.migrations.up");

                if (upSqlToken?.Type == JTokenType.String)
                {
                    table.WithSQLUp(upSqlToken.ToString());
                }
                else if (upSqlToken?.Type == JTokenType.Array)
                {
                    foreach (var sql in upSqlToken.Select(c => c.ToString()))
                    {
                        table.WithSQLUp(sql);
                    }
                }

                tables.Add(entity.Name, table);


            }

            //  if (this.options.GenerateDTO)
            {

                foreach (var entityDefinition in manifest.SelectToken("$.entities").OfType<JProperty>())
                {

                    var table = tables[entityDefinition.Name];

                    var parentName = entityDefinition.Value.SelectToken("$.TPT")?.ToString() ?? entityDefinition.Value.SelectToken("$.TPC")?.ToString(); 
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
                            var type = (attributeDefinition.Value.SelectToken("$.type.type")?.ToString() ?? attributeDefinition.Value.SelectToken("$.type")?.ToString())?.ToLower();
                            var isprimaryKey = attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;
                            var isPrimaryField = attributeDefinition.Value.SelectToken("$.isPrimaryField")?.ToObject<bool>() ?? false;
                            var attributeKey = attributeDefinition.Name;
                            var schemaName = attributeDefinition.Value.SelectToken("$.schemaName").ToString();
                            var logicalName = attributeDefinition.Value.SelectToken("$.logicalName").ToString();

                            //if (isprimaryKey && !string.IsNullOrEmpty(parentName))
                            //    continue;

                            if (type == "choices")
                                continue;

                            if (attributeDefinition.Value.SelectToken("$.metadataOnly")?.ToObject<bool>() == true )
                            {
                                /*
                                 * When the poly lookup is split, then there is generted additional 
                                 * reference tabels to link things together and migrations is set to false indicating that it can be skipped
                                 */
                                continue;
                            }


                            var propertyInfo = table
                                .AddProperty(attributeKey, schemaName, logicalName, type)
                                .WithExternalHash(HashExtensions.Sha256(attributeDefinition.Value.ToString()))
                                .WithExternalTypeHash(HashExtensions.Sha256(attributeDefinition.Value.SelectToken("$.type")?.ToString()))
                                .WithDescription(attributeDefinition.Value.SelectToken("$.type.description")?.ToString() ?? attributeDefinition.Value.SelectToken("$.description")?.ToString())
                                .WithMigrationColumnProvider(new ManifestColumnMigrationColumnResolver(attributeDefinition.Value))
                                .WithMaxLength((typeObj?.SelectToken("$.sql.maxLength") ?? typeObj?.SelectToken("$.maxLength"))?.ToObject<int>())
                                .Required(attributeDefinition.Value.SelectToken("$.type.required")?.ToObject<bool>() ?? attributeDefinition.Value.SelectToken("$.isRequired")?.ToObject<bool>() ?? false)
                                .RowVersion((attributeDefinition.Value.SelectToken("$.isRowVersion")?.ToObject<bool>() ?? false));


                            if (isprimaryKey)
                            {

                                propertyInfo.PrimaryKey();

                            }

                            if (isPrimaryField)
                            {

                                propertyInfo.PrimaryField();

                            }

                            if (type == "lookup" || type == "polylookup")
                            {
                                if (typeObj.SelectToken("$.inline")?.ToObject<bool>() ?? false)
                                {
                                    /*
                                     * When the poly lookup is inline, then there is generated additional lookups
                                     * and this should be considered a guid? property.
                                     */
                                    continue;
                                }

                              

                                propertyInfo
                                        .LookupTo(
                                            tables[attributeDefinition.Value.SelectToken("$.type.referenceType")?.ToString()],
                                            //attributeDefinition.Value.SelectToken("$.type.foreignKey")?.ToObject<ForeignKeyInfo>(),
                                            attributeDefinition.Value.SelectToken("$.type.cascade.delete")?.ToObject(options.ReferentialActionType),
                                            attributeDefinition.Value.SelectToken("$.type.cascade.update")?.ToObject(options.ReferentialActionType))
                                        .WithIndex(attributeDefinition.Value.SelectToken("$.type.index") != null ?
                                            attributeDefinition.Value.SelectToken("$.type.index")?.ToObject<IndexInfo>() ?? new IndexInfo { Unique = true } : null);

                                

                            }

                            if (type == "choice")
                            {
                                var choices = attributeDefinition.Value.SelectToken("$.type.options").OfType<JProperty>().ToDictionary(optionPro => choiceEnumBuilder.GetLiteralName(optionPro.Name), optionPro => (optionPro.Value.Type == JTokenType.Object ? optionPro.Value["value"] : optionPro.Value).ToObject<int>());
                                propertyInfo.AddChoiceOptions(choiceEnumBuilder.GetEnumName(attributeDefinition.Value, this.options.Namespace), choices);
                                // propertyInfo.prop
                            }



                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Failed to generate field {attributeDefinition.Name}:{ex.ToString()}", ex);
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
                    if(entity.Name == "Identity")
                    {

                    }
                    //  var a = table.Builder.CreateTypeInfo();
                    var schema = entity.Value.SelectToken("$.schema")?.ToString() ?? options.Schema ?? "dbo";
                    var result = this.options.DTOAssembly?.GetTypes().FirstOrDefault(t => t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute attr && attr.LogicalName == table.LogicalName && (this.options.SkipValidateSchemaNameForRemoteTypes || string.Equals(attr.Schema, schema, StringComparison.OrdinalIgnoreCase)))?.GetTypeInfo();
                     
                    this.options.EntityDTOConfigurations[table.CollectionSchemaName] = table.CreateConfigurationTypeInfo();
                    this.options.EntityDTOs[table.CollectionSchemaName] = result ?? table.CreateTypeInfo();
                }

            }

            return (CreateDynamicMigration(dynamicCodeService, manifest),
                tables.Values.TSort(d=>d.Dependencies).Select(entity => entity.CreateMigrationType(this.options.Namespace, this.options.MigrationName, this.options.PartOfMigration)).Select(entity => Activator.CreateInstance(entity) as IDynamicTable).ToArray());



        }
    }
}
