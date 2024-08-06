using EAVFramework.Extensions;
using EAVFW.Extensions.Manifest.SDK;
using EAVFW.Extensions.Manifest.SDK.DTO;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using static EAVFramework.Shared.TypeHelper;

namespace EAVFramework.Shared.V2
{
    public static class KeyValuePairLinqExtensions
    {
        public static IEnumerable<KeyValuePair<TKey, TTargetValue>> OfType<TKey, TValue, TTargetValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            foreach (var item in source)
            {
                if (item.Value is TTargetValue converted)
                    yield return new KeyValuePair<TKey, TTargetValue>(item.Key, converted);
            }
        }

        public static IEnumerable<KeyValuePair<TKey, TTargetValue>> OfType<TKey, TTargetValue>(
         this IEnumerable source)
        {
            foreach (var item in source)
            {

                if (item is KeyValuePair<TKey, object> kvp && kvp.Value is TTargetValue converted)
                    yield return new KeyValuePair<TKey, TTargetValue>(kvp.Key, converted);
            }
        }

    }

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
            var a = Contraints.GetOrAdd(name, new InterfaceShadowBuilderContraintContainer() { Order = Contraints.Count });
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

                var builders = Builder.DefineGenericParameters(Contraints.OrderBy(c => c.Value.Order).Select(c => c.Key).ToArray());
                foreach (var contraintbuilder in builders)
                {
                    var container = Contraints[contraintbuilder.Name];

                    var types = Contraints[contraintbuilder.Name].Constraints.ToArray();

                    var contraints = types.Select(c =>
                    {

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
                        var ger = string.Join(", ", a.Select(aa =>
                        {

                            if (aa.IsGenericType)
                                return $"{aa.Name}<{string.Join(",", aa.GetGenericArguments().Select(ga => ga.Name))}>";

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
        private readonly DynamicCodeService dynamicCodeService;
        private readonly ManifestServiceOptions options;
        private readonly IChoiceEnumBuilder choiceEnumBuilder;



        public ManifestService(DynamicCodeService dynamicCodeService, ManifestServiceOptions options, IChoiceEnumBuilder choiceEnumBuilder = null)
        {
            this.dynamicCodeService = dynamicCodeService;
            this.options = options;
            this.choiceEnumBuilder = choiceEnumBuilder ?? new DefaultChoiceEnumBuilder();
        }
        internal Type CreateDynamicMigration(DynamicCodeService dynamicCodeService, MigrationDefinition migration)
        {
            var asmb = dynamicCodeService.CreateAssemblyBuilder(options.ModuleName, options.Namespace);


            TypeBuilder migrationType =
                                         asmb.Module.DefineType($"{options.Namespace}.Migration{this.options.MigrationName}", TypeAttributes.Public, typeof(DynamicMigration));

            ;

            var attributeBuilder = new CustomAttributeBuilder(Resolve(() => typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) }), "MigrationAttributeCtor"), new object[] { this.options.MigrationName });
            migrationType.SetCustomAttribute(attributeBuilder);



            ConstructorBuilder entityTypeCtorBuilder =
                 migrationType.DefineConstructor(MethodAttributes.Public,
                                    CallingConventions.Standard, new[] { typeof(MigrationDefinition), typeof(IDynamicTable[]) });

            var entityTypeCtorBuilderIL = entityTypeCtorBuilder.GetILGenerator();
            var basector = typeof(DynamicMigration).GetConstructor(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance, null, new[] { typeof(MigrationDefinition), typeof(IDynamicTable[]) }, null);

            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_1);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_2);
            entityTypeCtorBuilderIL.Emit(OpCodes.Call, basector);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ret);


            //Assembly = builder;

            var type = migrationType.CreateTypeInfo();
            return type;
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
            foreach (var entity in manifest.SelectToken("$.entities").OfType<JProperty>())
            {

                var schema = entity.Value.SelectToken("$.schema")?.ToString() ?? options.Schema ?? "dbo";
                var result = this.options.DTOAssembly?.GetTypes().FirstOrDefault(t => t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute attr && attr.LogicalName == entity.Value.SelectToken("$.logicalName").ToString() && (this.options.SkipValidateSchemaNameForRemoteTypes || string.Equals(attr.Schema, schema, StringComparison.OrdinalIgnoreCase)))?.GetTypeInfo();


                var table = builder.WithTable(entity.Name,
                    tableSchemaname: entity.Value.SelectToken("$.schemaName").ToString(),
                    tableLogicalName: entity.Value.SelectToken("$.logicalName").ToString(),
                    tableCollectionSchemaName: entity.Value.SelectToken("$.collectionSchemaName").ToString(),
                     entity.Value.SelectToken("$.schema")?.ToString() ?? options.Schema,
                     entity.Value.SelectToken("$.abstract") != null,
                     entity.Value.SelectToken("$.mappingStrategy")?.ToObject<MappingStrategy>()
                    ).External(entity.Value.SelectToken("$.external")?.ToObject<bool>() ?? false, result);



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
                    if (entity.Name == "Identity")
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
                tables.Values.TSort(d => d.Dependencies).Select(entity => entity.CreateMigrationType(this.options.Namespace, this.options.MigrationName, this.options.PartOfMigration)).Select(entity => Activator.CreateInstance(entity) as IDynamicTable).ToArray());



        }
        private Dictionary<string, Stopwatch> Measurements { get; set; } = new Dictionary<string, Stopwatch>();
        public void StartMeasurement(string name)
        {
            if (!Measurements.ContainsKey(name))
                Measurements[name] = new Stopwatch();
            Measurements[name].Start();
        }
        public void StopMeasurement(string name)
        {
            if (!Measurements.ContainsKey(name))
                return;
            Measurements[name].Stop();
        }

        public (Type migrationType, IDynamicTable[] tables) BuildDynamicModel(DynamicCodeService dynamicCodeService, MigrationDefinition migration)
        {

            var builder = dynamicCodeService.CreateAssemblyBuilder(this.options.ModuleName, this.options.Namespace);
            var options = dynamicCodeService.Options;

            // var tables = new Dictionary<string, DynamicTableBuilder>();

            //foreach (var (entityKey, entity) in migration.GetNewEntities())
            //{
            //    var schema = entity.Schema ?? options.Schema ?? "dbo";


            //    StartMeasurement("ResolveDTOTypes");
            //    //We will find a DTO generated entity type for this entity if defined.
            //    var result = this.options.DTOAssembly
            //        ?.GetTypes()
            //        .FirstOrDefault(t => t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute attr && attr.LogicalName == entity.LogicalName && (this.options.SkipValidateSchemaNameForRemoteTypes || string.Equals(attr.Schema, schema, StringComparison.OrdinalIgnoreCase)))?.GetTypeInfo();
            //    StopMeasurement("ResolveDTOTypes");


            //    StartMeasurement("InitializeTables");
            //    var table = builder.WithTable(entityKey,
            //        tableSchemaname: entity.SchemaName,
            //        tableLogicalName: entity.LogicalName,
            //        tableCollectionSchemaName: entity.CollectionSchemaName,
            //         entity.Schema ?? options.Schema,
            //         entity.Abstract ?? false,
            //         entity.MappingStrategy
            //        ).External(entity.External ?? false, result);



            //    tables.Add(entityKey, table);

            //    StopMeasurement("InitializeTables");

            //}

            //foreach (var (entityKey, entity) in migration.GetNewEntities())
            //{

            //    var table = tables[entityKey];

            //    var parentName = entityDefinition.Value.SelectToken("$.TPT")?.ToString() ?? entityDefinition.Value.SelectToken("$.TPC")?.ToString();
            //    if (!string.IsNullOrEmpty(parentName))
            //    {
            //        table.WithBaseEntity(tables[parentName]);

            //    }

            //}

            var migrationType = CreateDynamicMigration(dynamicCodeService, migration);


            return (migrationType,
                CreateMigrationTables(migration, dynamicCodeService, builder));

            //    tables.Values.TSort(d => d.Dependencies).Select(entity => entity.CreateMigrationType(this.options.Namespace, this.options.MigrationName, this.options.PartOfMigration)).Select(entity => Activator.CreateInstance(entity) as IDynamicTable).ToArray());

        }

        public MethodInfo[] GetPrimaryKeys(EntityDefinition entity, Type columnsCLRType)
        {
            var properties = entity.Attributes.Values.OfType<AttributeObjectDefinition>();

            var primaryKeys = properties.Where(p => p.IsPrimaryKey ?? false)
             .Where(p => columnsCLRType.GetProperty(p.SchemaName) != null) // members.ContainsKey(p.LogicalName))
             .Select(p => columnsCLRType.GetProperty(p.SchemaName).GetMethod)
             .ToArray();

            return primaryKeys;
        }
        public bool IsAttributeLookup(AttributeObjectDefinition attribute)
        {
            return string.Equals(attribute.AttributeType.Type, "lookup", StringComparison.OrdinalIgnoreCase);
        }



        public string[] GetForeignKeys(Dictionary<string, EntityDefinition> entities, EntityDefinition entity)
        {
            var properties = entity.GetAllProperties(entities).Values.OfType<AttributeObjectDefinition>(); ; // entity.Attributes.Values.OfType<AttributeObjectDefinition>();

            var fKeys = properties.Where(IsAttributeLookup) // entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                                                            //  .Where(attribute => attribute.Value.SelectToken("$.type.type")?.ToString() == "lookup")
                                                            // .Where(attribute => members.ContainsKey(attribute.Value.SelectToken("$.logicalName")?.ToString()))

                 .Select(attribute => $"FK_{entity.CollectionSchemaName}_{entities[attribute.AttributeType.ReferenceType].CollectionSchemaName}_{attribute.SchemaName}".Replace(" ", ""))
                 .OrderBy(n => n)
                 .ToArray();
            return fKeys;
        }
        public ForeignKeyModel[] GetForeignKeys(Dictionary<string, EntityDefinition> entities, EntityDefinition entity, Type columnsCLRType, CascadeAction referentialActionNoAction)
        {

            var properties = entity.Attributes.Values.OfType<AttributeObjectDefinition>();

            var fKeys = properties.Where(IsAttributeLookup) // entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                                                            //  .Where(attribute => attribute.Value.SelectToken("$.type.type")?.ToString() == "lookup")
                                                            // .Where(attribute => members.ContainsKey(attribute.Value.SelectToken("$.logicalName")?.ToString()))
                 .Where(attribute => columnsCLRType.GetProperty(attribute.SchemaName) != null)
                 .Where(attribute => entities[attribute.AttributeType.ReferenceType].GetMappingStrategy(entities) == MappingStrategy.TPT)
                 .Select(attribute => new ForeignKeyModel
                 {
                     Name = $"FK_{entity.CollectionSchemaName}_{entities[attribute.AttributeType.ReferenceType].CollectionSchemaName}_{attribute.SchemaName}".Replace(" ", ""),
                     AttributeSchemaName = attribute.SchemaName,  //attribute.Value.SelectToken("$.schemaName").ToString(),
                     PropertyGetMethod = columnsCLRType.GetProperty(attribute.SchemaName).GetMethod,
                     ReferenceType = entities[attribute.AttributeType.ReferenceType],
                     OnDeleteCascade = (ReferentialAction)(attribute.AttributeType?.Cascades?.OnDelete ?? referentialActionNoAction),
                     OnUpdateCascade = (ReferentialAction)(attribute.AttributeType?.Cascades?.OnUpdate ?? referentialActionNoAction),
                     // ForeignKey = attribute.ForeignKey
                 }).OrderBy(n => n.Name)
                 .ToArray();
            return fKeys;
        }
        public class ForeignKeyModel
        {
            public string Name { get; set; }
            public string AttributeSchemaName { get; set; }
            public MethodInfo PropertyGetMethod { get; set; }
            public EntityDefinition ReferenceType { get; set; }
            public ReferentialAction OnDeleteCascade { get; set; }
            public ReferentialAction OnUpdateCascade { get; set; }
        }



        private Dictionary<PropertyInfo, object> BuildParametersForcolumn(ILGenerator entityCtorBuilderIL, AttributeObjectDefinition propertyInfo, MethodInfo method, string tableName = null, string schema = null)
        {
            // The following parameters can be set from the typeobject
            // Column<T>([CanBeNullAttribute] string type = null, bool? unicode = null, int? maxLength = null, bool rowVersion = false, [CanBeNullAttribute] string name = null, bool nullable = false, [CanBeNullAttribute] object defaultValue = null, [CanBeNullAttribute] string defaultValueSql = null, [CanBeNullAttribute] string computedColumnSql = null, bool? fixedLength = null, [CanBeNullAttribute] string comment = null, [CanBeNullAttribute] string collation = null, int? precision = null, int? scale = null, bool? stored = null)
            //   type:
            //     The database type of the column.
            //
            //   unicode:
            //     Indicates whether or not the column will store Unicode data.
            //
            //   maxLength:
            //     The maximum length for data in the column.
            //
            //   rowVersion:
            //     Indicates whether or not the column will act as a rowversion/timestamp concurrency
            //     token.
            //
            //   name:
            //     The column name.
            //
            //   nullable:
            //     Indicates whether or not the column can store null values.
            //
            //   defaultValue:
            //     The default value for the column.
            //
            //   defaultValueSql:
            //     The SQL expression to use for the column's default constraint.
            //
            //   computedColumnSql:
            //     The SQL expression to use to compute the column value.
            //
            //   fixedLength:
            //     Indicates whether or not the column is constrained to fixed-length data.
            //
            //   comment:
            //     A comment to be applied to the column.
            //
            //   collation:
            //     A collation to be applied to the column.
            //
            //   precision:
            //     The maximum number of digits for data in the column.
            //
            //   scale:
            //     The maximum number of decimal places for data in the column.
            //
            //   stored:
            //     Whether the value of the computed column is stored in the database or not.
            //
            // Type parameters:
            //   T:
            //     The CLR type of the column.
            //




            //
            // Summary:
            //     Builds an Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation
            //     to add a new column to a table.
            //
            // Parameters:
            //   name:
            //     The column name.
            //
            //   table:
            //     The name of the table that contains the column.
            //
            //   type:
            //     The store/database type of the column.
            //
            //   unicode:
            //     Indicates whether or not the column can contain Unicode data, or null if not
            //     specified or not applicable.
            //
            //   maxLength:
            //     The maximum length of data that can be stored in the column, or null if not specified
            //     or not applicable.
            //
            //   rowVersion:
            //     Indicates whether or not the column acts as an automatic concurrency token, such
            //     as a rowversion/timestamp column in SQL Server.
            //
            //   schema:
            //     The schema that contains the table, or null if the default schema should be used.
            //
            //   nullable:
            //     Indicates whether or not the column can store null values.
            //
            //   defaultValue:
            //     The default value for the column.
            //
            //   defaultValueSql:
            //     The SQL expression to use for the column's default constraint.
            //
            //   computedColumnSql:
            //     The SQL expression to use to compute the column value.
            //
            //   fixedLength:
            //     Indicates whether or not the column is constrained to fixed-length data.
            //
            //   comment:
            //     A comment to associate with the column.
            //
            //   collation:
            //     A collation to apply to the column.
            //
            //   precision:
            //     The maximum number of digits that is allowed in this column, or null if not specified
            //     or not applicable.
            //
            //   scale:
            //     The maximum number of decimal places that is allowed in this column, or null
            //     if not specified or not applicable.
            //
            //   stored:
            //     Whether the value of the computed column is stored in the database or not.
            //
            // Type parameters:
            //   T:
            //     The CLR type that the column is mapped to.
            //
            // Returns:
            //     A builder to allow annotations to be added to the operation.




            var options = dynamicCodeService.Options;
            var parameters = new Dictionary<PropertyInfo, object>();

            var locals = new Dictionary<Type, LocalBuilder>
            {
                [typeof(bool?)] = entityCtorBuilderIL.DeclareLocal(typeof(bool?)),
                [typeof(int?)] = entityCtorBuilderIL.DeclareLocal(typeof(int?)),

            };




            foreach (var arg1 in method.GetParameters())
            {

                var argName = arg1.Name;
                if (argName == "name")
                    argName = "columnName";


                switch (argName)
                {
                    case "comment" when !string.IsNullOrEmpty(propertyInfo.Description):

                        dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, propertyInfo.Description), arg1);

                        continue;
                }



                //var value = propertyInfo.GetColumnParam(argName);
                if (propertyInfo.AttributeType.SqlOptions?.TryGetValue(argName, out var value) ?? false)
                {
                    switch (value.ValueKind)
                    {
                        case System.Text.Json.JsonValueKind.String: // string stringvalue:
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, value.GetString()), arg1);

                            break;
                        case System.Text.Json.JsonValueKind.Number:
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldc_I4, value.GetInt32()), arg1);

                            break;

                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(value.GetBoolean() ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0), arg1);
                            break;

                        default:
                            if (Nullable.GetUnderlyingType(arg1.ParameterType) != null)
                            {
                                entityCtorBuilderIL.Emit(OpCodes.Ldloca_S, locals[arg1.ParameterType].LocalIndex);
                                entityCtorBuilderIL.Emit(OpCodes.Initobj, arg1.ParameterType);
                                entityCtorBuilderIL.Emit(OpCodes.Ldloc, locals[arg1.ParameterType]);
                            }
                            else
                            {
                                entityCtorBuilderIL.Emit(OpCodes.Ldnull);
                            }
                            break;
                    }


                }
                else
                {
                    //  var hasMaxLength = propertyInfo.MaxLength.HasValue;

                    switch (argName)
                    {


                        case "maxLength" when propertyInfo.AttributeType.MaxLength.HasValue:

                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldc_I4, propertyInfo.AttributeType.MaxLength.Value), arg1);

                            AddParameterComparison(parameters, argName, propertyInfo.AttributeType.MaxLength.Value);

                            break;
                        case "table" when !string.IsNullOrEmpty(tableName): entityCtorBuilderIL.Emit(OpCodes.Ldstr, tableName); break;
                        case "schema" when !string.IsNullOrEmpty(schema): dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, schema), arg1); break;
                        case "columnName": entityCtorBuilderIL.Emit(OpCodes.Ldstr, propertyInfo.SchemaName); break;
                        case "nullable" when (propertyInfo.IsPrimaryKey ?? false):
                        case "nullable" when options.RequiredSupport && ((propertyInfo.AttributeType.Required ?? false) || (propertyInfo.IsRequired ?? false)):
                        case "nullable" when (propertyInfo.IsRowVersion):
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldc_I4_0), arg1);
                            break;
                        case "nullable":
                            entityCtorBuilderIL.Emit(OpCodes.Ldc_I4_1);
                            break;
                        case "type" when string.Equals(propertyInfo.AttributeType.Type, "multilinetext", StringComparison.OrdinalIgnoreCase):
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, "nvarchar(max)"), arg1);
                            break;

                        case "type" when string.Equals(propertyInfo.AttributeType.Type, "text", StringComparison.OrdinalIgnoreCase) && !propertyInfo.AttributeType.MaxLength.HasValue:
                        case "type" when string.Equals(propertyInfo.AttributeType.Type, "string", StringComparison.OrdinalIgnoreCase) && !propertyInfo.AttributeType.MaxLength.HasValue:
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, $"nvarchar({((propertyInfo.IsPrimaryField) ? 255 : 100)})"), arg1);
                            break;
                        case "rowVersion" when propertyInfo.IsRowVersion:
                            entityCtorBuilderIL.Emit(OpCodes.Ldc_I4_1);


                            break;

                        default:
                            if (Nullable.GetUnderlyingType(arg1.ParameterType) != null)
                            {
                                entityCtorBuilderIL.Emit(OpCodes.Ldloca_S, locals[arg1.ParameterType].LocalIndex);
                                entityCtorBuilderIL.Emit(OpCodes.Initobj, arg1.ParameterType);
                                entityCtorBuilderIL.Emit(OpCodes.Ldloc, locals[arg1.ParameterType]);
                            }
                            else if (arg1.ParameterType == typeof(bool))
                            {
                                entityCtorBuilderIL.Emit(Convert.ToBoolean(arg1.DefaultValue) == true ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                            }
                            else
                            {

                                entityCtorBuilderIL.Emit(OpCodes.Ldnull);
                            }

                            break;
                    }


                }
                //
                //new ColumnsBuilder(null).Column<Guid>(
                //    type:"",unicode:false,maxLength:0,rowVersion:false,name:"a",nullable:false,defaultValue:null,defaultValueSql:null,
                //    computedColumnSql:null, fixedLength:false,comment:"",collation:"",precision:0,scale:0,stored:false);
                //type:


            }
            return parameters;

            void AddParameterComparison<T>(Dictionary<PropertyInfo, object> _parameters, string argName, T value)
            {
                var prop = typeof(EntityMigrationColumnsAttribute).GetProperties().FirstOrDefault(x => string.Equals(x.Name, argName, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                    _parameters[prop] = value;
            }
        }


        public void EmitAddColumn(ILGenerator il, string table, string schema, AttributeObjectDefinition attributeDefinition)
        {

            var clrType = dynamicCodeService.TypeMapper.GetCLRType(attributeDefinition.AttributeType.Type);

            var method = dynamicCodeService.Options.MigrationsBuilderAddColumn.MakeGenericMethod(clrType);


            il.Emit(OpCodes.Ldarg_1); //first argument
                                      //MigrationsBuilderAddColumn




            BuildParametersForcolumn(il, attributeDefinition, method, table, schema);





            il.Emit(OpCodes.Callvirt, method);
            il.Emit(OpCodes.Pop);

        }

        public void AddForeignKey(string EntityCollectionSchemaName, string schema, ILGenerator UpMethodIL,
          AttributeObjectDefinition field, EntityDefinition referenceType)
        {
            //
            // Summary:
            //     Builds an Microsoft.EntityFrameworkCore.Migrations.Operations.AddForeignKeyOperation
            //     to add a new foreign key to a table.
            //
            // Parameters:
            //   name:
            //     The foreign key constraint name.
            //
            //   table:
            //     The table that contains the foreign key.
            //
            //   column:
            //     The column that is constrained.
            //
            //   principalTable:
            //     The table to which the foreign key is constrained.
            //
            //   schema:
            //     The schema that contains the table, or null if the default schema should be used.
            //
            //   principalSchema:
            //     The schema that contains principal table, or null if the default schema should
            //     be used.
            //
            //   principalColumn:
            //     The column to which the foreign key column is constrained, or null to constrain
            //     to the primary key column.
            //
            //   onUpdate:
            //     The action to take on updates.
            //
            //   onDelete:
            //     The action to take on deletes.
            //
            // Returns:
            //     A builder to allow annotations to be added to the operation.



            UpMethodIL.Emit(OpCodes.Ldarg_1);

            // var entityName = attributeDefinition.Value.SelectToken("$.type.referenceType");

            var principalSchema = referenceType.Schema ?? dynamicCodeService.Options.Schema ?? "dbo"; // dynamicPropertyBuilder.ReferenceType.Schema; // manifest.SelectToken($"$.entities['{entityName}'].schema")?.ToString() ?? options.Schema ?? "dbo";
            var principalTable = referenceType.CollectionSchemaName;// manifest.SelectToken($"$.entities['{entityName}'].pluralName").ToString().Replace(" ", "");
            var principalColumn = referenceType.Attributes.Values.OfType<AttributeObjectDefinition>().Single(p => p.IsPrimaryKey ?? false).SchemaName; //  manifest.SelectToken($"$.entities['{entityName}'].attributes").OfType<JProperty>()
                                                                                                                                                       // .Single(a => a.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false).Name.Replace(" ", "");

            var onDeleteCascade = field.AttributeType?.Cascades?.OnDelete ?? dynamicCodeService.Options.ReferentialActionNoAction;
            var onUpdateCascade = field.AttributeType?.Cascades?.OnUpdate ?? dynamicCodeService.Options.ReferentialActionNoAction;

            foreach (var arg1 in dynamicCodeService.Options.MigrationsBuilderAddForeignKey.GetParameters())
            {
                var argName = arg1.Name.ToLower();

                switch (argName)
                {
                    case "table" when !string.IsNullOrEmpty(EntityCollectionSchemaName): UpMethodIL.Emit(OpCodes.Ldstr, EntityCollectionSchemaName); break;
                    case "schema" when !string.IsNullOrEmpty(schema): UpMethodIL.Emit(OpCodes.Ldstr, schema); break;
                    case "name": UpMethodIL.Emit(OpCodes.Ldstr, $"FK_{EntityCollectionSchemaName}_{referenceType.CollectionSchemaName}_{field.SchemaName}".Replace(" ", "")); break;
                    case "column": UpMethodIL.Emit(OpCodes.Ldstr, field.SchemaName); break;
                    case "principalschema": UpMethodIL.Emit(OpCodes.Ldstr, principalSchema); break;
                    case "principaltable": UpMethodIL.Emit(OpCodes.Ldstr, principalTable); break;
                    case "principalcolumn": UpMethodIL.Emit(OpCodes.Ldstr, principalColumn); break;
                    case "onupdate": UpMethodIL.Emit(OpCodes.Ldc_I4, (int)onUpdateCascade); break;
                    case "ondelete": UpMethodIL.Emit(OpCodes.Ldc_I4, (int)onDeleteCascade); break;

                    default:

                        UpMethodIL.Emit(OpCodes.Ldnull);
                        break;
                }
            }


            UpMethodIL.Emit(OpCodes.Callvirt, dynamicCodeService.Options.MigrationsBuilderAddForeignKey);
            UpMethodIL.Emit(OpCodes.Pop);
        }

        class TableResult
        {
            public HashSet<string> Dependencies { get; set; } = new HashSet<string>();
            public string Key { get; set; }
            public Type Type { get; set; }
        }
        internal IDynamicTable[] CreateMigrationTables(MigrationDefinition migration, DynamicCodeService dynamicCodeService, DynamicAssemblyBuilder builder)
        {
            var results = new Dictionary<string, TableResult>();
            var module = builder.Module;
            var migrationName = options.MigrationName.Replace(".", "_");
            foreach (var pair in migration.Entities)
            {
                var result = new TableResult
                {
                    Key = pair.Key
                };
                results.Add(pair.Key, result);



                var entityKey = pair.Key;
                var entity = pair.Value;
                var entityMigration = migration.GetEntityMigration(entityKey);

                foreach (var field in GetFields(entity).Where(v => IsAttributeLookup(v.Value)))
                {
                    result.Dependencies.Add(field.Value.AttributeType.ReferenceType);
                }

                var schema = entity.Schema ?? dynamicCodeService.Options.Schema ?? "dbo";

                var entityTypeBuilder = module.DefineType($"{builder.Namespace}.{entity.CollectionSchemaName}Builder_{migrationName}", TypeAttributes.Public);

                CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { entity.LogicalName });
                entityTypeBuilder.SetCustomAttribute(EntityAttributeBuilder);


                entityTypeBuilder.AddInterfaceImplementation(dynamicCodeService.Options.DynamicTableType);



                if (entity.External ?? false)
                {
                    var UpMethod = entityTypeBuilder.DefineMethod("Up", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { dynamicCodeService.Options.MigrationBuilderCreateTable.DeclaringType });

                    var UpMethodIL = UpMethod.GetILGenerator();

                    UpMethodIL.Emit(OpCodes.Ret);

                    var DownMethod = entityTypeBuilder.DefineMethod("Down", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { dynamicCodeService.Options.MigrationBuilderDropTable.DeclaringType });
                    var DownMethodIL = DownMethod.GetILGenerator();

                    DownMethodIL.Emit(OpCodes.Ret);

                    result.Type = entityTypeBuilder.CreateTypeInfo();
                    continue;
                }


                {
                    var UpMethod = entityTypeBuilder.DefineMethod("Up", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { dynamicCodeService.Options.MigrationBuilderCreateTable.DeclaringType });
                    var migrationBuilder = new MigrationBuilderBuilder(builder, UpMethod, dynamicCodeService, dynamicCodeService.Options);



                    if (migration.IsTableNew(entityKey))
                    {
                        var (columnsCLRType, columnsctor, members) = CreateColumnsType(
                            builder, entity.SchemaName, entity.LogicalName, migrationName, true,
                            entity.GetProperties(migration.Entities).Values.OfType<AttributeObjectDefinition>().ToList());

                        var columsMethod = entityTypeBuilder.DefineMethod("Columns", MethodAttributes.Public, columnsCLRType, new[] { dynamicCodeService.Options.ColumnsBuilderType });

                        var columsMethodIL = columsMethod.GetILGenerator();
                        columsMethodIL.Emit(OpCodes.Ldarg_1);
                        columsMethodIL.Emit(OpCodes.Newobj, columnsctor);
                        columsMethodIL.Emit(OpCodes.Ret);


                        var ConstraintsMethod = entityTypeBuilder.DefineMethod("Constraints",
                            MethodAttributes.Public, null, new[] { dynamicCodeService.Options.CreateTableBuilderType.MakeGenericType(columnsCLRType) });
                        var ConstraintsMethodIL = ConstraintsMethod.GetILGenerator();



                        dynamicCodeService.EmitPropertyService.CreateTableImpl(entity.CollectionSchemaName, schema, columnsCLRType, columsMethod, ConstraintsMethod, migrationBuilder.UpMethodIL);

                        dynamicCodeService.LookupPropertyBuilder.CreateLookupIndexes(migrationBuilder.UpMethodIL, entity, dynamicCodeService.Options.Schema ?? "dbo");



                        var primaryKeys = GetPrimaryKeys(entity, columnsCLRType);
                        var foreignKeys = GetForeignKeys(migration.Entities, entity, columnsCLRType,
                            dynamicCodeService.Options.ReferentialActionNoAction);


                        if (primaryKeys.Any() || foreignKeys.Any())
                        {
                            ConstraintsMethodIL.DeclareLocal(typeof(ParameterExpression));
                        }

                        HandlePrimaryKeys(dynamicCodeService, builder, entity, columnsCLRType, ConstraintsMethodIL, primaryKeys);

                        HandleForeignKeys(dynamicCodeService, builder, columnsCLRType, ConstraintsMethodIL, foreignKeys);


                        ConstraintsMethodIL.Emit(OpCodes.Ret);

                    }
                    else
                    {

                       
                        var migrationStrategy = entityMigration.MappingStrategyChange();



                        foreach (var newField in entityMigration.GetNewAttributes().OfType<AttributeObjectDefinition>())
                        {
                            var required = (newField.IsRequired ?? false) || (newField.AttributeType.Required ?? false);

                            //We cant add a required column to existing table, rely on it being altered after data is set.
                            //this is a case when we are changing from TPT to TPC 
                            newField.IsRequired = newField.AttributeType.Required = false;
                            EmitAddColumn(migrationBuilder.UpMethodIL, entity.CollectionSchemaName, schema, newField);
                            newField.IsRequired = newField.AttributeType.Required = required;

                            if (IsFieldLookup(newField))
                            {
                                var refrenceType = migration.Entities[newField.AttributeType.ReferenceType];

                                if (refrenceType.GetMappingStrategy(migration.Entities) == MappingStrategy.TPT)
                                    AddForeignKey(entity.CollectionSchemaName, schema, migrationBuilder.UpMethodIL, newField,
                                        refrenceType);

                                if (newField.AttributeType.IndexInfo != null)
                                    dynamicCodeService.LookupPropertyBuilder.CreateLoopupIndex(migrationBuilder.UpMethodIL, entity.CollectionSchemaName, entity.Schema ?? dynamicCodeService.Options.Schema ?? "dbo",
                                        newField.SchemaName, newField.AttributeType.IndexInfo);

                            }

                        }

                        if (migrationStrategy == MappingStrategyChangeEnum.TPT2TPC && !(entity.Abstract??false))
                        {
                            var columnsToMove = entityMigration.GetAttributesMovingFromBase()
                                .OfType<AttributeObjectDefinition>()
                                .Where(c=>!c.IsRowVersion)
                                .ToArray();

                           
                                var columnsToMoveSql = string.Join("\n",
                                    columnsToMove
                                    .Select(attr => $"\t[{schema}].[{entity.CollectionSchemaName}].[{attr.SchemaName}] = BaseRecords.[{attr.SchemaName}],"));

                                var upSql1 = $"""
                                        UPDATE
                                        [{schema}].[{entity.CollectionSchemaName}]
                                        SET
                                        {columnsToMoveSql.Trim(',')}
                                        FROM
                                            [{schema}].[{entity.CollectionSchemaName}] Records
                                        INNER JOIN
                                            [{schema}].[{entity.GetBaseEntity(migration.Source.Entities).CollectionSchemaName}] BaseRecords
                                        ON 
                                            records.Id = BaseRecords.Id;
                                        """;
                            

                            EmitSQLUp(migrationBuilder.UpMethodIL, upSql1);

                            foreach (var existingField in columnsToMove)
                            {

                                if (IsFieldRequired(existingField))
                                    EmitAlterColumn(migrationBuilder.UpMethodIL, entity.CollectionSchemaName, schema, existingField);
                            }
                        }


                        foreach (var existingField in entityMigration.GetExistingFields())
                        {

                            if (existingField.HasChanged())
                            {
                                EmitAlterColumn(migrationBuilder.UpMethodIL, entity.CollectionSchemaName, schema, existingField.Target);
                                
                            }

                            if (IsFieldLookup(existingField.Target) && existingField.HasCascadeChanges())
                            {
                                var referenceType = migration.Entities[existingField.Target.AttributeType.ReferenceType];
                                migrationBuilder.DropForeignKey(entity.CollectionSchemaName, schema,
                                    $"FK_{entity.CollectionSchemaName}_{referenceType.CollectionSchemaName}_{existingField.Target.SchemaName}".Replace(" ", ""));
                                AddForeignKey(entity.CollectionSchemaName, schema, migrationBuilder.UpMethodIL, existingField.Target,
                                 referenceType);
                            }

                            {

                                if (IsFieldLookup(existingField.Target)
                                    && !string.IsNullOrEmpty(existingField.Target.AttributeType.ReferenceType) && migration.Entities[existingField.Target.AttributeType.ReferenceType] is EntityDefinition referenceType
                                    && (referenceType.Abstract ?? false)
                                    && migration.GetEntityMigration(existingField.Target.AttributeType.ReferenceType).MappingStrategyChange() == MappingStrategyChangeEnum.TPT2TPC)
                                {
                                    migrationBuilder.DropForeignKey(entity.CollectionSchemaName, schema,
                                         $"FK_{entity.CollectionSchemaName}_{referenceType.CollectionSchemaName}_{existingField.Target.SchemaName}".Replace(" ", ""));
                                }

                            }

                        }





                    }

               
                    // var fields = entity.GetAllProperties(migration.Entities).Values.OfType<AttributeObjectDefinition>().ToArray();
                    foreach (var key in entityMigration.GetNewKeys())
                    { 

                        var props = key.Value;
                        var name = key.Key;
                    
                        var colums = props.Select(p => entity.GetField(p,migration.Entities).SchemaName).ToArray();
                        migrationBuilder.CreateIndex(entity.CollectionSchemaName,entity.Schema ?? dynamicCodeService.Options.Schema ?? "dbo",
                            name, true, colums);

                    }



                    migrationBuilder.UpMethodIL.Emit(OpCodes.Ret);


                    var DownMethod = entityTypeBuilder.DefineMethod("Down", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { dynamicCodeService.Options.MigrationBuilderDropTable.DeclaringType });
                    var DownMethodIL = DownMethod.GetILGenerator();


                    DownMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                    DownMethodIL.Emit(OpCodes.Ldstr, entity.CollectionSchemaName); //Constant
                    DownMethodIL.Emit(OpCodes.Ldstr, entity.Schema ?? dynamicCodeService.Options.Schema ?? "dbo");
                    DownMethodIL.Emit(OpCodes.Callvirt, dynamicCodeService.Options.MigrationBuilderDropTable);
                    DownMethodIL.Emit(OpCodes.Pop);

                    DownMethodIL.Emit(OpCodes.Ret);

                    result.Type = entityTypeBuilder.CreateTypeInfo();

                }



                //TODO : SQL UP




            }



            return results.Keys.TSort(x => results[x].Dependencies)
                .Select(entity => Activator.CreateInstance(results[entity].Type) as IDynamicTable).ToArray();

        }

        private bool IsFieldRequired(AttributeObjectDefinition source)
        {
            return source.IsRequired ?? source.AttributeType.Required ?? false;
        }

        public bool IsFieldLookup(AttributeObjectDefinition source)
        {
            return string.Equals(source.AttributeType.Type, "lookup", StringComparison.OrdinalIgnoreCase)
                || string.Equals(source.AttributeType.Type, "polylookup", StringComparison.OrdinalIgnoreCase);
        }

        public void EmitAlterColumn(ILGenerator UpMethodIL, string table, string schema, AttributeObjectDefinition attributeDefinition)
        {
            var clrType = dynamicCodeService.TypeMapper.GetCLRType(attributeDefinition.AttributeType.Type);
            var method = dynamicCodeService.Options.MigrationsBuilderAlterColumn.MakeGenericMethod(clrType);


            UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                                              //MigrationsBuilderAddColumn

            BuildParametersForcolumn(UpMethodIL, attributeDefinition, method, table, schema);

            UpMethodIL.Emit(OpCodes.Callvirt, method);
            UpMethodIL.Emit(OpCodes.Pop);
        }

        private void EmitSQLUp(ILGenerator UpMethodIL, string upSql1)
        {
            UpMethodIL.Emit(OpCodes.Ldarg_1);  //first argument
            UpMethodIL.Emit(OpCodes.Ldstr, upSql1);


            UpMethodIL.Emit(OpCodes.Ldc_I4_0);
            UpMethodIL.Emit(OpCodes.Callvirt, dynamicCodeService.Options.MigrationBuilderSQL);
            UpMethodIL.Emit(OpCodes.Pop);
        }


        private bool IsBaseMember(Dictionary<string, EntityDefinition> entities, EntityDefinition entity, string key, out EntityDefinition parentEntity)
        {
            parentEntity = null;

            var parent = entity.GetParentEntity(entities);
            while (parent != null)
            {
                if (GetFields(parent).Any(p => p.Key == key))
                {
                    parentEntity = parent;
                    return true;
                }
                parent = parent.GetParentEntity(entities);
            }
            return false;
        }

        public IEnumerable<KeyValuePair<string, AttributeObjectDefinition>> GetFields(EntityDefinition entityDefinition)
        {
            return entityDefinition.Attributes.OfType<string, AttributeDefinitionBase, AttributeObjectDefinition>()
                .OrderByDescending(c => c.Value.IsPrimaryKey).ThenByDescending(c => c.Value.IsPrimaryField).ThenBy(c => c.Value.LogicalName).ToArray();
        }

        private static void HandleForeignKeys(DynamicCodeService dynamicCodeService, DynamicAssemblyBuilder builder, Type columnsCLRType, ILGenerator ConstraintsMethodIL, ForeignKeyModel[] foreignKeys)
        {
            if (foreignKeys.Any())
            {


                /**
                 * 
                 * TPT for base classe
                 *  constraints: table =>
                    {
                        table.PrimaryKey("PK_Identities", x => x.Id);
                        table.ForeignKey(
                            name: "FK_Identities_Identities_CreatedById",
                            column: x => x.CreatedById,
                            principalTable: "Identities",
                            principalColumn: "Id");
                        table.ForeignKey(
                            name: "FK_Identities_Identities_ModifiedById",
                            column: x => x.ModifiedById,
                            principalTable: "Identities",
                            principalColumn: "Id");
                        table.ForeignKey(
                            name: "FK_Identities_Identities_OwnerId",
                            column: x => x.OwnerId,
                            principalTable: "Identities",
                            principalColumn: "Id");
                    });
                 *
                 * TPC for base class
                 *  constraints: table =>
                    {
                        table.PrimaryKey("PK_Identity", x => x.Id);
                    });
                 */
                foreach (var fk in foreignKeys)
                {
                    /**
                     * We will skip the FKs if its a TPC and base class. See above comment
                     */
                    if ((fk.ReferenceType.Abstract ?? false) && fk.ReferenceType.MappingStrategy == MappingStrategy.TPC)
                    {
                        continue;
                    }
                    ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, fk.Name);


                    dynamicCodeService.EmitPropertyService.WriteLambdaExpression(builder.Module, ConstraintsMethodIL, columnsCLRType, new[] { fk.PropertyGetMethod });// fk.Select(c => c.PropertyGetMethod).ToArray());

                    var createTableMethod = dynamicCodeService.Options.CreateTableBuilderType.MakeGenericType(columnsCLRType)
                        .GetMethod(dynamicCodeService.Options.CreateTableBuilderForeignKeyName, BindingFlags.Public | BindingFlags.Instance, null,
                            new[] {
                                typeof(string),
                                typeof(Expression<>).MakeGenericType(
                                    typeof(Func<,>).MakeGenericType(columnsCLRType, typeof(object))),
                                typeof(string),typeof(string),typeof(string),
                                dynamicCodeService.Options.ReferentialActionType,dynamicCodeService.Options.ReferentialActionType }, null);

                    var principalSchema = fk.ReferenceType.Schema ?? dynamicCodeService.Options.Schema ?? "dbo";
                    var principalTable = fk.ReferenceType.CollectionSchemaName;
                    var principalColumn = fk.ReferenceType.Attributes.Values.OfType<AttributeObjectDefinition>().SingleOrDefault(p => p.IsPrimaryKey ?? false)?.SchemaName;

                    if (string.IsNullOrEmpty(principalColumn))
                    {
                        throw new InvalidOperationException($"No reference type primary key defined for foreignkey {fk.ReferenceType.SchemaName} on {fk.Name}");
                    }

                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalTable);
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalColumn);
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalSchema);

                    ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, (int)fk.OnUpdateCascade); //OnUpdate
                    ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, (int)fk.OnDeleteCascade); //OnDelete


                    //
                    //onupdate
                    //ondelete
                    ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                    ConstraintsMethodIL.Emit(OpCodes.Pop);
                }
            }
        }

        private static void HandlePrimaryKeys(DynamicCodeService dynamicCodeService, DynamicAssemblyBuilder builder, EntityDefinition entity, Type columnsCLRType, ILGenerator ConstraintsMethodIL, MethodInfo[] primaryKeys)
        {
            if (primaryKeys.Any())
            {
                ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                ConstraintsMethodIL.Emit(OpCodes.Ldstr, $"PK_{entity.CollectionSchemaName}"); //PK Name

                dynamicCodeService.EmitPropertyService.WriteLambdaExpression(builder.Module, ConstraintsMethodIL, columnsCLRType, primaryKeys.Select(c => columnsCLRType.GetProperty(c.Name.Substring(4)).GetMethod).ToArray());

                var createTableMethod = dynamicCodeService.Options.CreateTableBuilderType.MakeGenericType(columnsCLRType).GetMethod(dynamicCodeService.Options.CreateTableBuilderPrimaryKeyName, BindingFlags.Public | BindingFlags.Instance, null,
                    new[] { typeof(string), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(columnsCLRType, typeof(object))) }, null);
                ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                ConstraintsMethodIL.Emit(OpCodes.Pop);

            }
        }

        public (Type, ConstructorBuilder, Dictionary<string, PropertyBuilder>) CreateColumnsType(DynamicAssemblyBuilder builder, string schemaName, string logicalName,
           string migrationName, bool partOfMigration, IReadOnlyCollection<AttributeObjectDefinition> props)
        {

            var members = new Dictionary<string, PropertyBuilder>();

            var columnsType = builder.Module.DefineType($"{builder.Namespace}.{schemaName}Columns_{migrationName.Replace(".", "_")}", TypeAttributes.Public);

            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { logicalName });
            columnsType.SetCustomAttribute(EntityAttributeBuilder);



            var dfc = columnsType.DefineDefaultConstructor(MethodAttributes.Public);

            ConstructorBuilder entityCtorBuilder =
               columnsType.DefineConstructor(MethodAttributes.Public,
                                  CallingConventions.Standard, new[] { dynamicCodeService.Options.ColumnsBuilderType });



            ILGenerator entityCtorBuilderIL = entityCtorBuilder.GetILGenerator();

            entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityCtorBuilderIL.Emit(OpCodes.Call, dfc);

            foreach (var propertyInfo in props)
            {
                var attributeLogicalName = propertyInfo.LogicalName;
                var attributeSchemaName = propertyInfo.SchemaName;

                var clrType = dynamicCodeService.TypeMapper.GetCLRType(propertyInfo.AttributeType.Type);
                if (clrType == null)
                    continue;

                var method = dynamicCodeService.Options.ColumnsBuilderColumnMethod.MakeGenericMethod(clrType);


                entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
                entityCtorBuilderIL.Emit(OpCodes.Ldarg_1);

                var (attProp, attField) = dynamicCodeService.EmitPropertyService.CreateProperty(columnsType, attributeSchemaName, dynamicCodeService.Options.OperationBuilderAddColumnOptionType); //CreateProperty(entityType, attributeSchemaName, options.OperationBuilderAddColumnOptionType);



                var columparams = BuildParametersForcolumn(entityCtorBuilderIL, propertyInfo, method);

                entityCtorBuilderIL.Emit(OpCodes.Callvirt, method);
                entityCtorBuilderIL.Emit(OpCodes.Call, attProp.SetMethod);

                members[attributeLogicalName] = attProp;
            }

            entityCtorBuilderIL.Emit(OpCodes.Ret);

            var entityClrType = columnsType.CreateTypeInfo();
            return (entityClrType, entityCtorBuilder, members);

        }
    }


    public static class ManifestQueryExtensions
    {
        public static EntityDefinition GetBaseEntity(this EntityDefinition entity, Dictionary<string, EntityDefinition> entities)
        {
            var parent = entity.GetParentEntity(entities);
            while (parent != null)
            {
                entity = parent;
                parent = entity.GetParentEntity(entities);
            }
            return entity;
        }
        public static EntityDefinition GetParentEntity(this EntityDefinition entity, Dictionary<string, EntityDefinition> entities)
        {

            var parentName = entity.TPC ?? entity.TPT;
            if (!string.IsNullOrEmpty(parentName))
            {
                return entities[parentName];

            }
            return null;
        }

        public static AttributeObjectDefinition GetField(this EntityDefinition entity,string key, Dictionary<string, EntityDefinition> entities)
        {
           
            
            while (entity != null)
            {
                if (entity.Attributes.ContainsKey(key) && entity.Attributes[key] is AttributeObjectDefinition attr)
                    return attr;

                entity = entity.GetParentEntity(entities);
            }

            throw new KeyNotFoundException($"Field {key} not found in entity {entity.CollectionSchemaName} or its parent entities.");


        }

        public static Dictionary<string, AttributeDefinitionBase> GetAllProperties(this EntityDefinition entity, Dictionary<string, EntityDefinition> entities)
        {

            var properties = new Dictionary<string, AttributeDefinitionBase>(entity.Attributes);
            var parent = entity.GetParentEntity(entities);
            while (parent != null)
            {
                foreach (var prop in parent.Attributes)
                {
                    if (!properties.ContainsKey(prop.Key))
                        properties.Add(prop.Key, prop.Value);
                }
                parent = parent.GetParentEntity(entities);
            }

            return properties;


        }
        public static IEnumerable<AttributeDefinitionBase> GetNewAttributes(this MigrationEntityDefinition migrationEntity)
        {
            var target = migrationEntity.Target;
            var source = migrationEntity.Source;

            var targetAttributes = target.GetProperties(migrationEntity.MigrationDefinition.Target.Entities);
            var sourceAttributes = source.GetProperties(migrationEntity.MigrationDefinition.Source.Entities);


            return targetAttributes.Where(e => !sourceAttributes.ContainsKey(e.Key)).Select(c => c.Value);
        }
        public static IEnumerable<AttributeDefinitionBase> GetAttributesMovingFromBase(this MigrationEntityDefinition migrationEntity)
        {
            var target = migrationEntity.Target;
            var source = migrationEntity.Source;

            var targetAttributes = target.GetProperties(migrationEntity.MigrationDefinition.Target.Entities);
            var sourceAttributes = source.GetProperties(migrationEntity.MigrationDefinition.Source.Entities);



            return targetAttributes.Where(e => !sourceAttributes.ContainsKey(e.Key) && !target.Attributes.ContainsKey(e.Key)).Select(c => c.Value);
        }

        public static Dictionary<string, AttributeObjectDefinition> GetProperties(this EntityDefinition entity,
            Dictionary<string, EntityDefinition> entities)
        {
            return  (entity.GetMappingStrategy(entities) == MappingStrategy.TPC ?
                               entity.GetAllProperties(entities) : entity.Attributes)
                               .OfType<string,AttributeDefinitionBase,AttributeObjectDefinition>()
                               .OrderByDescending(c => c.Value.IsPrimaryKey)
                               .ThenByDescending(c => c.Value.IsPrimaryField)
                               .ThenBy(c => c.Value.LogicalName)
                               .ToDictionary(k=>k.Key,v=>v.Value);

        }
        public static Dictionary<string, string[]> GetNewKeys(this MigrationEntityDefinition migrationEntity)
        {    
            return migrationEntity.Target?.Keys?
                .Where(kv=> !(migrationEntity.Source?.Keys?.ContainsKey(kv.Key) ??false))
                .ToDictionary(k=>k.Key,v=>v.Value) ?? new Dictionary<string, string[]>();
        }
        public static MappingStrategyChangeEnum MappingStrategyChange(this MigrationEntityDefinition migrationEntity)
        {



            var source = migrationEntity.Source.GetMappingStrategy(migrationEntity.MigrationDefinition.Source.Entities);
            var target = migrationEntity.Target.GetMappingStrategy(migrationEntity.MigrationDefinition.Target.Entities);

            if (source == target)
            {
                return MappingStrategyChangeEnum.None;
            }

            return source switch
            {

                MappingStrategy.TPT when target == MappingStrategy.TPC => MappingStrategyChangeEnum.TPT2TPC,
                MappingStrategy.TPC when target == MappingStrategy.TPT => MappingStrategyChangeEnum.TPC2TPT,

                _ => throw new NotImplementedException($"{source} => {target}"),
            };

        }


        public static MappingStrategy GetMappingStrategy(this EntityDefinition entity, Dictionary<string, EntityDefinition> entities)
        {
            // TPC, TPT, TPH
            // TPC = Table Per Concrete Type
            // TPT = Table Per Type
            // TPH = Table Per Hierarchy
            // TPT is the default
            // The stategy is stored on the base class, and if not provided
            // its indicated based on the navigation properties TPT,TPC on entity
            var mappingstrategy = entity.MappingStrategy ?? (!string.IsNullOrEmpty(entity.TPT) ?
                MappingStrategy.TPT : !string.IsNullOrEmpty(entity.TPC) ? MappingStrategy.TPC : MappingStrategy.TPT);


            var parent = GetParentEntity(entity, entities);

            if (parent != null)
            {
                mappingstrategy = parent.MappingStrategy ?? mappingstrategy;
                parent = parent.GetParentEntity(entities);
            }

            return mappingstrategy;


        }
    }
}
