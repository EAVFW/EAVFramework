using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Shared
{
    public class PrimaryFieldAttribute : Attribute
    {

    }
    public class EntityMigrationAttribute : Attribute
    {
        public string LogicalName { get; set; }
        public string MigrationName { get; set; }
        public string RawUpMigration { get; set; }
    }
    public class EntityMigrationColumnsAttribute : Attribute
    {
        public string LogicalName { get; set; }
        public string MigrationName { get; set; }
        public string AttributeLogicalName { get; set; }
    }

    public class EntityAttribute : Attribute
    {
        public string LogicalName { get; set; }
        public string SchemaName { get; set; }
        public string CollectionSchemaName { get; set; }

        public bool IsBaseClass { get; set; }
    }

    
    public class AttributeAttribute : Attribute
    {
        public string LogicalName { get; set; }
        public string SchemaName { get; set; }
        public string CollectionSchemaName { get; set; }
    }

    public class EntityDTOAttribute : Attribute
    {
        public string LogicalName { get; set; }
        public string Schema { get; set; }
    }
    public class BaseEntityAttribute : Attribute
    {

    }
    [AttributeUsage( AttributeTargets.Class,AllowMultiple =true)]
    public class GenericTypeArgumentAttribute : Attribute
    {
        public string ManifestKey { get; set; }
        public string ArgumentName { get; set; }
    }

    public class CodeGeneratorOptions
    {
        public ModuleBuilder myModule { get; set; }
        public string Namespace { get; set; }
        public string migrationName { get; set; }

        public MethodInfo MigrationBuilderCreateTable { get; set; }
        public MethodInfo MigrationBuilderSQL { get; set; }
        public Type ColumnsBuilderType { get; set; }
        public Type CreateTableBuilderType { get; set; }
        public Type EntityTypeBuilderType { get; set; }
        public MethodInfo EntityTypeBuilderPropertyMethod { get; set; }
        public MethodInfo EntityTypeBuilderToTable { get; set; }
        public MethodInfo EntityTypeBuilderHasKey { get; set; }
        public string Schema { get; set; }
        public ConstructorInfo ForeignKeyAttributeCtor { get; internal set; }
        public ConstructorInfo InverseAttributeCtor { get; internal set; }


        public Dictionary<string, Type> EntityDTOs { get; internal set; } = new Dictionary<string, Type>();
        public ConcurrentDictionary<string, TypeBuilder> EntityDTOsBuilders { get; internal set; } = new ConcurrentDictionary<string, TypeBuilder>();
        public Dictionary<string, Type> EntityDTOConfigurations { get; internal set; } = new Dictionary<string, Type>();
        public Type OperationBuilderAddColumnOptionType { get; internal set; }
        public MethodInfo ColumnsBuilderColumnMethod { get; internal set; }
        public MethodInfo LambdaBase { get; internal set; }

        public Type EntityConfigurationInterface { get; internal set; }
        public string EntityConfigurationConfigureName { get; internal set; }
        public ConstructorInfo JsonPropertyNameAttributeCtor { get; internal set; }
        public ConstructorInfo JsonConverterAttributeCtor { get; internal set; }
        public Type ChoiceConverter { get; internal set; }

        public ConstructorInfo JsonPropertyAttributeCtor { get; internal set; }
        public Type DynamicTableType { get; internal set; }
        public string CreateTableBuilderPrimaryKeyName { get; internal set; }
        public string CreateTableBuilderForeignKeyName { get; internal set; }
        public Type ReferentialActionType { get; internal set; }
        public int ReferentialActionNoAction { get; internal set; }
        public Type DynamicMigrationType { get; internal set; }
        public Type DynamicTableArrayType { get; internal set; }

        public ConstructorInfo MigrationAttributeCtor { get; internal set; }
        public MethodInfo MigrationBuilderDropTable { get; internal set; }
        public Assembly DTOAssembly { get; set; }
        public Type[] DTOBaseClasses { get; internal set; }

        public Action<JToken, PropertyBuilder> OnDTOTypeGeneration { get; set; }
        public bool GeneratePoco { get; set; } = false;
        public MethodInfo EntityTypeBuilderHasAlternateKey { get;  set; }
        public MethodInfo MigrationBuilderCreateIndex { get; internal set; }
        public MethodInfo MigrationBuilderDropIndex { get; internal set; }
        public MethodInfo IsRowVersionMethod { get; internal set; }
        public MethodInfo IsRequiredMethod { get; internal set; }
        public MethodInfo HasConversionMethod { get; internal set; }
        public MethodInfo HasPrecisionMethod { get; internal set; }
        public MethodInfo ValueGeneratedOnUpdate { get; set; }
        public bool GenerateDTO { get; set; } = true;
        public bool PartOfMigration { get;  set; }
        public MethodInfo MigrationsBuilderAddColumn { get;  set; }
        public bool SkipValidateSchemaNameForRemoteTypes { get;  set; }
        public bool UseOnlyExpliciteExternalDTOClases { get;  set; }
        public bool RequiredSupport { get; set; } = true;
    }

    public interface ICodeGenerator
    {

    }
    public static class TSortExt
    {
        public static IEnumerable<T> TSort<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle = false)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            foreach (var item in source)
                Visit(item, visited, sorted, dependencies, throwOnCycle);

            return sorted;
        }

        private static void Visit<T>(T item, HashSet<T> visited, List<T> sorted, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle)
        {
            if (!visited.Contains(item))
            {
                visited.Add(item);

                foreach (var dep in dependencies(item))
                    Visit(dep, visited, sorted, dependencies, throwOnCycle);

                sorted.Add(item);
            }
            else
            {
                if (throwOnCycle && !sorted.Contains(item))
                    throw new Exception("Cyclic dependency found");
            }
        }
    }
    public class IndexInfo
    {
        public bool Unique { get; set; } = true;
        public string Name { get; set; }
    }

    public interface IChoiceEnumBuilder
    {
        string GetEnumName(CodeGeneratorOptions options, JToken attributeDefinition);
    }
    public class DefaultChoiceEnumBuilder : IChoiceEnumBuilder
    {
        public string GetEnumName(CodeGeneratorOptions options, JToken attributeDefinition)
        {
            var optionSetDefinedName = attributeDefinition.SelectToken("$.type.name");
            var schemaName= attributeDefinition.SelectToken("$.schemaName");
            
            return $"{options.Namespace}.{optionSetDefinedName ?? schemaName+"Options"}".Replace(" ", "");
        }
    }
    public interface IManifestTypeMapper
    {
        Type GetCLRType(string manifestType);
    }
    public class DefaultManifestTypeMapper : IManifestTypeMapper
    {
        public Type GetCLRType(string manifestType)
        {
            switch (manifestType.ToLower())
            {
                case "text":
                case "string":
                case "multilinetext":
                    return typeof(string);
                case "guid":
                case "lookup":
                    return typeof(Guid?);
                case "integer":
                case "int":

                    return typeof(int?);
                case "decimal":
                    return typeof(decimal?);
                case "datetime":
                case "date":
                    return typeof(DateTime?);
                case "boolean":
                    return typeof(bool?);
                case "choice":
                    return typeof(int?);
                case "customer":
                    return typeof(Guid?);
                case "binary":
                    return typeof(byte[]);
                case "number":
                    return typeof(double?);
            }
            return null;
        }
    }

    public class CodeGenerator : ICodeGenerator
    {

        private readonly CodeGeneratorOptions options;
        private readonly IChoiceEnumBuilder choiceEnumBuilder;
        private readonly IManifestTypeMapper typemapper;

        public CodeGenerator(CodeGeneratorOptions options,
            IChoiceEnumBuilder choiceEnumBuilder = null,
            IManifestTypeMapper typemapper = null)
        {
            this.options = options;
            this.choiceEnumBuilder=choiceEnumBuilder??new DefaultChoiceEnumBuilder();
            this.typemapper=typemapper??new DefaultManifestTypeMapper();
        }

        public Dictionary<string, StringBuilder> methodBodies = new Dictionary<string, StringBuilder>();

        public TypeBuilder BuildMigrationDefinition(ModuleBuilder builder, string migrationName, JToken before, JToken after)
        {
            TypeBuilder entityTypeBuilder =
             builder.DefineType($"{options.Namespace}.{migrationName}Builder", TypeAttributes.Public);

            entityTypeBuilder.AddInterfaceImplementation(options.DynamicTableType);

            //var schema = entityDefinition.Value.SelectToken("$.schema")?.ToString() ?? options.Schema ?? "dbo";


            return entityTypeBuilder;
        }
        public TypeBuilder BuildEntityDefinition(ModuleBuilder builder, JToken manifest, JProperty entityDefinition)
        {
            var EntitySchameName = entityDefinition.Name.Replace(" ", "");
            var EntityCollectionSchemaName = (entityDefinition.Value.SelectToken("$.pluralName")?.ToString() ?? EntitySchameName).Replace(" ", "");
            //   AppDomain myDomain = AppDomain.CurrentDomain;
            //   AssemblyName myAsmName = new AssemblyName("MigrationTable" + entity.Name + "Assembly");

            //  var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
            //    AssemblyBuilderAccess.Run);



            //  ModuleBuilder myModule =
            //    builder.DefineDynamicModule(myAsmName.Name);
            if (options.GenerateDTO)
            {
                CreateDTO(builder, EntityCollectionSchemaName, EntitySchameName, entityDefinition, manifest);
                CreateDTOConfiguration(builder, EntityCollectionSchemaName, EntitySchameName, entityDefinition.Value as JObject, manifest);
            }

            var logicalName = entityDefinition.Value.SelectToken("$.logicalName").ToString();
            var hasPriorEntity = builder.GetTypes().FirstOrDefault(t => !IsPendingTypeBuilder(t)&& t.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migrationAttribute && migrationAttribute.LogicalName == logicalName);

            
          
            TypeBuilder entityTypeBuilder =
            builder.DefineType($"{options.Namespace}.{EntityCollectionSchemaName}Builder_{options.migrationName.Replace(".", "_")}", TypeAttributes.Public);


            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] {logicalName });
            entityTypeBuilder.SetCustomAttribute(EntityAttributeBuilder);


            var upSqlToken = entityDefinition.Value.SelectToken("$.sql.migrations.up");
            string upSql = null;
            if(upSqlToken?.Type == JTokenType.String)
            {
                upSql=upSqlToken.ToString();
            }else if (upSqlToken?.Type == JTokenType.Array)
            {
                upSql = string.Join("\n", upSqlToken.Select(c => c.ToString()));
            }

            if (options.PartOfMigration)
            {
                CustomAttributeBuilder EntityMigrationAttributeBuilder = new CustomAttributeBuilder(
                    typeof(EntityMigrationAttribute).GetConstructor(new Type[] { }), 
                    new object[] { },
                    new[] {
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.LogicalName)),
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.MigrationName)) ,
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.RawUpMigration)) 
                    },


                    new[] { logicalName, options.migrationName, upSql });
                entityTypeBuilder.SetCustomAttribute(EntityMigrationAttributeBuilder);
            }

            entityTypeBuilder.AddInterfaceImplementation(options.DynamicTableType);

            var isExternal = entityDefinition.Value.SelectToken("$['external']")?.ToObject<bool>() ?? false;

            if (isExternal)
            {
                var UpMethod = entityTypeBuilder.DefineMethod("Up", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderCreateTable.DeclaringType });

                var UpMethodIL = UpMethod.GetILGenerator();

                UpMethodIL.Emit(OpCodes.Ret);

                var DownMethod = entityTypeBuilder.DefineMethod("Down", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderDropTable.DeclaringType });
                var DownMethodIL = DownMethod.GetILGenerator();

                DownMethodIL.Emit(OpCodes.Ret);

                return entityTypeBuilder;
            }
            else
            {

                var (columnsCLRType, columnsctor, members) = CreateColumnsType(manifest, EntitySchameName, EntityCollectionSchemaName, entityDefinition.Value as JObject, builder);


                //var (nameBuilder, namefield) = CreateProperty(entityTypeBuilder, nameof(IDynamicTable<>.Name), typeof(string), 
                //    methodAttributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual);
                //var (schemaBuilder, schemaField) = CreateProperty(entityTypeBuilder, nameof(IDynamicTable<>.Schema), typeof(string),
                //    methodAttributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual);

                //         ConstructorBuilder entityTypeCtorBuilder =
                //entityTypeBuilder.DefineConstructor(MethodAttributes.Public,
                //                   CallingConventions.Standard, Type.EmptyTypes);
                //         // Generate IL for the method. The constructor stores its argument in the private field.
                //         ILGenerator entityTypeCtorBuilderIL = entityTypeCtorBuilder.GetILGenerator();
                //         entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_0);
                //         entityTypeCtorBuilderIL.Emit(OpCodes.Ldstr, entity.Name);
                //         entityTypeCtorBuilderIL.Emit(OpCodes.Stfld, namefield);
                //         entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_0);
                //         entityTypeCtorBuilderIL.Emit(OpCodes.Ldstr, "dbo");
                //         entityTypeCtorBuilderIL.Emit(OpCodes.Stfld, schemaField);

                //         entityTypeCtorBuilderIL.Emit(OpCodes.Ret);




                var columsMethod = entityTypeBuilder.DefineMethod("Columns", MethodAttributes.Public, columnsCLRType, new[] { options.ColumnsBuilderType });

                var columsMethodIL = columsMethod.GetILGenerator();
                columsMethodIL.Emit(OpCodes.Ldarg_1);
                columsMethodIL.Emit(OpCodes.Newobj, columnsctor);
                columsMethodIL.Emit(OpCodes.Ret);


                var ConstraintsMethod = entityTypeBuilder.DefineMethod("Constraints", MethodAttributes.Public, null, new[] { options.CreateTableBuilderType.MakeGenericType(columnsCLRType) });
                var ConstraintsMethodIL = ConstraintsMethod.GetILGenerator();

                var primaryKeys = entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                .Where(attribute => attribute.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)
                 .Where(attribute => members.ContainsKey(attribute.Name))
                .Select(attribute => members[attribute.Name].GetMethod)
                .ToArray();


                var fKeys = entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                   .Where(attribute => attribute.Value.SelectToken("$.type.type")?.ToString() == "lookup")
                     .Where(attribute => members.ContainsKey(attribute.Name))
                   .Select(attribute => new { AttributeSchemaName = attribute.Value.SelectToken("$.schemaName").ToString(), PropertyGetMethod = members[attribute.Name].GetMethod, EntityName = attribute.Value.SelectToken("$.type.referenceType").ToString(), ForeignKey = attribute.Value.SelectToken("$.type.foreignKey") })
                   .ToArray();

                if (primaryKeys.Any() || fKeys.Any())
                {
                    ConstraintsMethodIL.DeclareLocal(typeof(ParameterExpression));
                }

                if (primaryKeys.Any())
                {

                    ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, $"PK_{EntityCollectionSchemaName}"); //PK Name

                    WriteLambdaExpression(builder, ConstraintsMethodIL, columnsCLRType, primaryKeys);

                    var createTableMethod = options.CreateTableBuilderType.MakeGenericType(columnsCLRType).GetMethod(options.CreateTableBuilderPrimaryKeyName, BindingFlags.Public | BindingFlags.Instance, null,
                        new[] { typeof(string), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(columnsCLRType, typeof(object))) }, null);
                    ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                    ConstraintsMethodIL.Emit(OpCodes.Pop);
                }


                if (fKeys.Any())
                {
                    foreach (var fk in fKeys) //.GroupBy(c => c.EntityName))
                    {

                        //CreateTableBuilder
                        var entityName = fk.EntityName;
                        ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, $"FK_{EntityCollectionSchemaName}_{manifest.SelectToken($"$.entities['{entityName}'].pluralName")}_{fk.AttributeSchemaName}".Replace(" ", ""));

                        // Console.WriteLine($"FK_{EntityCollectionSchemaName}_{manifest.SelectToken($"$.entities['{entityName}'].pluralName")}_{fk.AttributeSchemaName}".Replace(" ", ""));


                        WriteLambdaExpression(builder, ConstraintsMethodIL, columnsCLRType, new[] { fk.PropertyGetMethod });// fk.Select(c => c.PropertyGetMethod).ToArray());

                        var createTableMethod = options.CreateTableBuilderType.MakeGenericType(columnsCLRType)
                            .GetMethod(options.CreateTableBuilderForeignKeyName, BindingFlags.Public | BindingFlags.Instance, null,
                                new[] {
                                typeof(string),
                                typeof(Expression<>).MakeGenericType(
                                    typeof(Func<,>).MakeGenericType(columnsCLRType, typeof(object))),
                                typeof(string),typeof(string),typeof(string),
                                options.ReferentialActionType,options.ReferentialActionType }, null);

                        var principalSchema = manifest.SelectToken($"$.entities['{entityName}'].schema")?.ToString() ?? options.Schema ?? "dbo";
                        var principalTable = manifest.SelectToken($"$.entities['{entityName}'].pluralName").ToString().Replace(" ", "");
                        var principalColumn = manifest.SelectToken($"$.entities['{entityName}'].attributes").OfType<JProperty>()
                            .Single(a => a.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false).Name.Replace(" ", "");

                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalTable);
                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalColumn);
                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalSchema);

                        ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, options.ReferentialActionNoAction);
                        ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, options.ReferentialActionNoAction);


                        //
                        //onupdate
                        //ondelete
                        ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                        ConstraintsMethodIL.Emit(OpCodes.Pop);
                    }
                }



                ConstraintsMethodIL.Emit(OpCodes.Ret);

                var schema = entityDefinition.Value.SelectToken("$.schema")?.ToString() ?? options.Schema ?? "dbo";

                var UpMethod = entityTypeBuilder.DefineMethod("Up", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderCreateTable.DeclaringType });

                var UpMethodIL = UpMethod.GetILGenerator();

                if (hasPriorEntity == null)
                {
                    CreateTableImpl(EntityCollectionSchemaName, schema, columnsCLRType, columsMethod, ConstraintsMethod, UpMethodIL);

                    //Create Indexes
                    //alternativ keys //TODO create dropindex
                    var keys = entityDefinition.Value.SelectToken("$.keys") as JObject;
                    if (keys != null)
                    {
                        foreach (var key in keys.OfType<JProperty>())
                        {
                            var props = key.Value.ToObject<string[]>();

                            UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                            UpMethodIL.Emit(OpCodes.Ldstr, key.Name); //Constant keyname 
                            UpMethodIL.Emit(OpCodes.Ldstr, EntityCollectionSchemaName); //Constant table name


                            UpMethodIL.Emit(OpCodes.Ldc_I4, props.Length); // Array length
                            UpMethodIL.Emit(OpCodes.Newarr, typeof(string));
                            for (var j = 0; j < props.Length; j++)
                            {
                                var attributeDefinition = entityDefinition.Value.SelectToken($"$.attributes['{props[j]}']");
                                var attributeSchemaName = attributeDefinition.SelectToken("$.schemaName")?.ToString();
                                UpMethodIL.Emit(OpCodes.Dup);
                                UpMethodIL.Emit(OpCodes.Ldc_I4, j);
                                UpMethodIL.Emit(OpCodes.Ldstr, attributeSchemaName);
                                UpMethodIL.Emit(OpCodes.Stelem_Ref);
                            }



                            UpMethodIL.Emit(OpCodes.Ldstr, schema); //Constant schema
                            UpMethodIL.Emit(OpCodes.Ldc_I4_1); //Constant unique=true
                            UpMethodIL.Emit(OpCodes.Ldnull); //Constant filter=null


                            UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderCreateIndex);
                            UpMethodIL.Emit(OpCodes.Pop);
                        }
                    }

                    //Create indexes from lookup fields.

                    foreach (JProperty attribute in entityDefinition.Value.SelectToken("$.attributes"))
                    {
                        if (attribute.Value.SelectToken("$.type.type")?.ToString() == "lookup" &&
                            (attribute.Value.SelectToken("$.type.index") != null))
                        {
                            var info = attribute.Value.SelectToken("$.type.index");
                            var indexInfo = info.Type == JTokenType.Object ? info.ToObject<IndexInfo>() : new IndexInfo { Unique = true };

                            UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                            UpMethodIL.Emit(OpCodes.Ldstr, indexInfo.Name ?? "IX_" + attribute.Value.SelectToken("$.schemaName")?.ToString()); //Constant keyname 
                            UpMethodIL.Emit(OpCodes.Ldstr, EntityCollectionSchemaName); //Constant table name


                            UpMethodIL.Emit(OpCodes.Ldc_I4_1); // Array length
                            UpMethodIL.Emit(OpCodes.Newarr, typeof(string));
                            UpMethodIL.Emit(OpCodes.Dup);
                            UpMethodIL.Emit(OpCodes.Ldc_I4_0);
                            UpMethodIL.Emit(OpCodes.Ldstr, attribute.Value.SelectToken("$.schemaName")?.ToString());
                            UpMethodIL.Emit(OpCodes.Stelem_Ref);


                            UpMethodIL.Emit(OpCodes.Ldstr, schema); //Constant schema
                            UpMethodIL.Emit(indexInfo.Unique ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0); //Constant unique=true
                            UpMethodIL.Emit(OpCodes.Ldnull); //Constant filter=null


                            UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderCreateIndex);
                            UpMethodIL.Emit(OpCodes.Pop);

                        }
                    }


                }
                else if (members.Any())
                {
                    foreach (var newMember in members)
                    {
                        var attributeDefinition = entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                            .FirstOrDefault(attribute => attribute.Name == newMember.Key);

                        var (typeObj, type) = GetTypeInfo(manifest, attributeDefinition);


                        var method = GetColumnForType(options.MigrationsBuilderAddColumn, type);
                        if (method == null)
                            continue;

                        UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                                                          //   MigrationsBuilderAddColumn


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




                        BuildParametersForcolumn(UpMethodIL, attributeDefinition, typeObj, type, method, EntityCollectionSchemaName, schema);

                        UpMethodIL.Emit(OpCodes.Callvirt, method);
                        UpMethodIL.Emit(OpCodes.Pop);
                    }
                }

              //  if (entityTypeBuilder.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migration && string.IsNullOrEmpty( migration.RawUpMigration))
                if(!string.IsNullOrEmpty(upSql))
                {
                   var alreadyExists= builder.GetTypes().FirstOrDefault(t =>
                        !IsPendingTypeBuilder(t) &&
                        t.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migrationAttribute &&
                        migrationAttribute.LogicalName == logicalName && migrationAttribute.RawUpMigration == upSql);

                    if (alreadyExists == null)
                    {

                        UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                        UpMethodIL.Emit(OpCodes.Ldstr, upSql);

                        UpMethodIL.Emit(OpCodes.Ldnull);
                        UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderSQL);
                        UpMethodIL.Emit(OpCodes.Pop);
                    }
                    
                }

                UpMethodIL.Emit(OpCodes.Ret);

                var DownMethod = entityTypeBuilder.DefineMethod("Down", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderDropTable.DeclaringType });
                var DownMethodIL = DownMethod.GetILGenerator();

                if (hasPriorEntity == null)
                {
                    DownMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                    DownMethodIL.Emit(OpCodes.Ldstr, EntityCollectionSchemaName); //Constant
                    DownMethodIL.Emit(OpCodes.Ldstr, schema);
                    DownMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderDropTable);
                    DownMethodIL.Emit(OpCodes.Pop);
                }
                DownMethodIL.Emit(OpCodes.Ret);

                //  var type = entityTypeBuilder.CreateTypeInfo();

                return entityTypeBuilder;
            }
        }
        public IDynamicTable[] GetTables(JToken manifest, ModuleBuilder builder)
        {
             
            var abstracts = manifest.SelectToken("$.entities").OfType<JProperty>()
                .Where(entity => entity.Value.SelectToken("$.abstract") != null)
                .Select(entity => this.BuildEntityDefinition(builder, manifest, entity).CreateTypeInfo()).ToArray();

         
            var entities = manifest.SelectToken("$.entities").OfType<JProperty>().ToArray();


            var builders = entities.TSort(v =>
                v.Value.SelectToken("$.attributes").OfType<JProperty>()
                .Where(a => a.Value.SelectToken("$.type.type")?.ToString()?.ToLower() == "lookup")
                .Select(a => entities.First(k => k.Name == a.Value.SelectToken("$.type.referenceType")?.ToString())))
                .Where(entity => entity.Value.SelectToken("$.abstract") == null)
                .Select(entity => this.BuildEntityDefinition(builder, manifest, entity)).ToArray();


            var tables = abstracts.Concat(builders.Select(entity => entity.CreateTypeInfo())).Select(entity => Activator.CreateInstance(entity) as IDynamicTable).ToArray();

            foreach (var entityDefinition in manifest.SelectToken("$.entities").OfType<JProperty>())
            {
                var EntitySchameName = entityDefinition.Name.Replace(" ", "");
                var EntityCollectionSchemaName = (entityDefinition.Value.SelectToken("$.pluralName")?.ToString() ?? EntitySchameName).Replace(" ", "");
                
                if (!options.EntityDTOs.ContainsKey(EntityCollectionSchemaName))
                    options.EntityDTOs[EntityCollectionSchemaName] =  
                        GetRemoteTypeIfExist(entityDefinition.Value) ??
                        options.EntityDTOsBuilders[EntitySchameName].CreateTypeInfo();

            }

            return tables;
        }



        internal Type CreateDynamicMigration(JToken manifest)
        {
            TypeBuilder migrationType =
                                         options.myModule.DefineType($"{options.Namespace}.Migration{options.migrationName}", TypeAttributes.Public, options.DynamicMigrationType);



            var attributeBuilder = new CustomAttributeBuilder(options.MigrationAttributeCtor, new object[] { options.migrationName });
            migrationType.SetCustomAttribute(attributeBuilder);

           

            ConstructorBuilder entityTypeCtorBuilder =
                 migrationType.DefineConstructor(MethodAttributes.Public,
                                    CallingConventions.Standard, new[] { typeof(JToken), options.DynamicTableArrayType });

            var entityTypeCtorBuilderIL = entityTypeCtorBuilder.GetILGenerator();
            var basector = options.DynamicMigrationType.GetConstructor(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance, null, new[] { typeof(JToken), options.DynamicTableArrayType }, null);

            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_1);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ldarg_2);
            entityTypeCtorBuilderIL.Emit(OpCodes.Call, basector);
            entityTypeCtorBuilderIL.Emit(OpCodes.Ret);


            //Assembly = builder;

            var type = migrationType.CreateTypeInfo();
            return type;
        }

        private void WriteLambdaExpression(ModuleBuilder builder, ILGenerator il, Type clrType, params MethodInfo[] getters)
        {


            // x=>x.id
            var GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static);
            var GetMethodFromHandle = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(RuntimeMethodHandle) }, null);
            var MemberExpression = typeof(Expression).GetMethod(nameof(Expression.Property), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(MethodInfo) }, null);

            var ExpressionBind = typeof(Expression).GetMethod(nameof(Expression.Bind), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(MethodInfo), typeof(Expression) }, null);
            var ExpressionMemberInit = typeof(Expression).GetMethod(nameof(Expression.MemberInit), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(NewExpression), typeof(MemberBinding[]) }, null);
            var ExpressionNew = typeof(Expression).GetMethod(nameof(Expression.New), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ConstructorInfo), typeof(IEnumerable<Expression>), typeof(MemberInfo[]) }, null);
            var ParameterExpression = typeof(Expression).GetMethod(nameof(Expression.Parameter), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type), typeof(string) }, null);

            var Lambda = options.LambdaBase.MakeGenericMethod(typeof(Func<,>).MakeGenericType(clrType, typeof(object)));

            //        IL_00fc: call class [System.Linq.Expressions]System.Linq.Expressions.Expression`1<!!0> [System.Linq.Expressions]System.Linq.Expressions.Expression::Lambda<class [System.Private.CoreLib]System.Func`2<class ColumnsTest, object>>(class [System.Linq.Expressions]System.Linq.Expressions.Expression, class [System.Linq.Expressions]System.Linq.Expressions.ParameterExpression[])

            il.Emit(OpCodes.Ldtoken, clrType);
            il.Emit(OpCodes.Call, GetTypeFromHandle);
            il.Emit(OpCodes.Ldstr, "x"); // x 
            il.Emit(OpCodes.Call, ParameterExpression);
            il.Emit(OpCodes.Stloc, 0);

            if (getters.Skip(1).Any())
            {
                var compositeKeyBuilder = builder.DefineType(clrType.Name + "Key");
                var compositeKeyParts = new Dictionary<string, PropertyBuilder>();
                foreach (var getmethod in getters)
                {
                    var (prop, field) = CreateProperty(compositeKeyBuilder, getmethod.Name.Substring("get_".Length), getmethod.ReturnType);
                    compositeKeyParts[getmethod.Name.Substring("get_".Length)] = prop;
                }
                var ctor = compositeKeyBuilder.DefineConstructor(MethodAttributes.Private, CallingConventions.Standard, getters.Select(c => c.ReturnType).ToArray());
                var ctorIL = ctor.GetILGenerator();
                ctorIL.Emit(OpCodes.Ret);
                var compositeKeyType = compositeKeyBuilder.CreateTypeInfo();


                // var newex = Expression.MemberInit(Expression.New)

                // il.Emit(OpCodes.Ldtoken, compositeKeyType);
                // il.Emit(OpCodes.Call, GetTypeFromHandle);
                //  il.Emit(OpCodes.Call, typeof(Expression).GetMethod(nameof(Expression.New), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null));
                //  il.Emit(OpCodes.Ldc_I4, getters.Length);
                //  il.Emit(OpCodes.Newarr, typeof(MemberBinding));
                //  var anoyCtor = compositeKeyType.GetConstructors().Single();

                il.Emit(OpCodes.Ldtoken, ctor);
                il.Emit(OpCodes.Ldtoken, compositeKeyType);

                var GetMethodFromHandleCtor = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }, null);

                il.Emit(OpCodes.Call, GetMethodFromHandleCtor);
                il.Emit(OpCodes.Castclass, typeof(ConstructorInfo));
                il.Emit(OpCodes.Ldc_I4, getters.Length);
                il.Emit(OpCodes.Newarr, typeof(Expression));


                for (var i = 0; i < getters.Length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldtoken, getters[i]);
                    il.Emit(OpCodes.Call, GetMethodFromHandle);
                    il.Emit(OpCodes.Castclass, typeof(MethodInfo));

                    il.Emit(OpCodes.Call, MemberExpression);
                    // il.Emit(OpCodes.Call, ExpressionBind);
                    il.Emit(OpCodes.Stelem_Ref);

                }

                il.Emit(OpCodes.Ldc_I4, getters.Length);
                il.Emit(OpCodes.Newarr, typeof(MemberInfo));



                for (var i = 0; i < getters.Length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);

                    il.Emit(OpCodes.Ldtoken, compositeKeyParts[getters[i].Name.Substring("get_".Length)].GetMethod);
                    il.Emit(OpCodes.Ldtoken, compositeKeyType);
                    il.Emit(OpCodes.Call, GetMethodFromHandleCtor);
                    il.Emit(OpCodes.Castclass, typeof(MethodInfo));

                    il.Emit(OpCodes.Stelem_Ref);

                }


                il.Emit(OpCodes.Call, ExpressionNew);



            }
            else
            {



                il.Emit(OpCodes.Ldloc, 0);
                il.Emit(OpCodes.Ldtoken, getters[0]);
                il.Emit(OpCodes.Call, GetMethodFromHandle);
                il.Emit(OpCodes.Castclass, typeof(MethodInfo));
                il.Emit(OpCodes.Call, MemberExpression);
                il.Emit(OpCodes.Ldtoken, typeof(object));

                il.Emit(OpCodes.Call, GetTypeFromHandle);
                il.Emit(OpCodes.Call, typeof(Expression).GetMethod(nameof(Expression.Convert), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(Type) }, null));

            }

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Newarr, typeof(ParameterExpression));
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, 0);
            il.Emit(OpCodes.Stelem_Ref);
            il.Emit(OpCodes.Call, Lambda);






        }


        private void CreateTableImpl(string entityCollectionName, string schema, Type columnsCLRType, MethodBuilder columsMethod, MethodBuilder ConstraintsMethod, ILGenerator UpMethodIL)
        {
            var createTableMethod = options.MigrationBuilderCreateTable.MakeGenericMethod(columnsCLRType);

            UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
            UpMethodIL.Emit(OpCodes.Ldstr, entityCollectionName); //Constant

            UpMethodIL.Emit(OpCodes.Ldarg_0); //this
            UpMethodIL.Emit(OpCodes.Ldftn, columsMethod);
            UpMethodIL.Emit(OpCodes.Newobj, typeof(Func<,>).MakeGenericType(options.ColumnsBuilderType, columnsCLRType).GetConstructors().Single());

            UpMethodIL.Emit(OpCodes.Ldstr, schema);

            UpMethodIL.Emit(OpCodes.Ldarg_0); //this
            UpMethodIL.Emit(OpCodes.Ldftn, ConstraintsMethod);
            UpMethodIL.Emit(OpCodes.Newobj, typeof(Action<>).MakeGenericType(options.CreateTableBuilderType.MakeGenericType(columnsCLRType)).GetConstructors().Single());

            UpMethodIL.Emit(OpCodes.Ldstr, "comment");

            UpMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
            UpMethodIL.Emit(OpCodes.Pop);

          


        }

        public virtual Type GetCLRType(ModuleBuilder myModule, JProperty entityDefinition, JToken attributeDefinition, JToken manifest, out string manifestType)
        {
            var typeObj = attributeDefinition.SelectToken("$.type");
            manifestType = typeObj.ToString();
            if (typeObj.Type == JTokenType.Object)
            {
                manifestType = typeObj.SelectToken("$.type").ToString();
            }

            manifestType = manifestType.ToLower();

            if (manifestType == "choice")
            {
                var enumName = choiceEnumBuilder.GetEnumName(options, attributeDefinition);
                var enumValue = myModule.DefineEnum(enumName, TypeAttributes.Public, typeof(int));
                foreach (JProperty optionPro in attributeDefinition.SelectToken("$.type.options"))
                {
                    enumValue.DefineLiteral(optionPro.Name.Replace(" ",""), (optionPro.Value.Type== JTokenType.Object ? optionPro.Value["value"] : optionPro.Value).ToObject<int>());

                }

                return  typeof(Nullable<>).MakeGenericType(enumValue.CreateTypeInfo());
            }

            return GetCLRType(manifestType);
        }
        public virtual Type GetCLRType(string manifestType)
        {
            return typemapper.GetCLRType(manifestType);
            
        }


        public void CreateDTOConfiguration(ModuleBuilder myModule, string entityCollectionSchemaName, string entitySchameName, JObject entityDefinition, JToken manifest)
        {
            var entityLogicalName = entityDefinition.SelectToken("$.logicalName").ToString();

            TypeBuilder entityTypeConfiguration = myModule.DefineType($"{options.Namespace}.{entitySchameName}Configuration", TypeAttributes.Public
                                                           | TypeAttributes.Class
                                                           | TypeAttributes.AutoClass
                                                           | TypeAttributes.AnsiClass
                                                           | TypeAttributes.Serializable
                                                           | TypeAttributes.BeforeFieldInit, null, new Type[] { options.EntityConfigurationInterface });


            entityTypeConfiguration.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { entityDefinition.SelectToken("$.logicalName").ToString() }));

            var Configure2Method = entityTypeConfiguration.DefineMethod(options.EntityConfigurationConfigureName, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.EntityTypeBuilderType });
            var ConfigureMethod2IL = Configure2Method.GetILGenerator();




            ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
            ConfigureMethod2IL.Emit(OpCodes.Ldstr, entityDefinition.SelectToken("$.pluralName").ToString().Replace(" ", "")); //Constant
            ConfigureMethod2IL.Emit(OpCodes.Ldstr, entityDefinition.SelectToken("$.schema")?.ToString() ?? options.Schema ?? "dbo"); //Constant
            ConfigureMethod2IL.Emit(OpCodes.Call, options.EntityTypeBuilderToTable);
            ConfigureMethod2IL.Emit(OpCodes.Pop);


            var isTablePerTypeChild = !string.IsNullOrEmpty(entityDefinition.SelectToken($"$.TPT")?.ToString());


            foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
            {
                if (attributeDefinition.Value.SelectToken("$.type.type")?.ToString().ToLower() == "choices")
                    continue;

                var attributeSchemaName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");
                var isprimaryKey = attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;
              
                if (isprimaryKey && !isTablePerTypeChild)
                {

                    ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4_1); // Array length
                    ConfigureMethod2IL.Emit(OpCodes.Newarr, typeof(string));
                    ConfigureMethod2IL.Emit(OpCodes.Dup);
                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4_0);
                    ConfigureMethod2IL.Emit(OpCodes.Ldstr, attributeSchemaName);
                    ConfigureMethod2IL.Emit(OpCodes.Stelem_Ref);
                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.EntityTypeBuilderHasKey);
                    ConfigureMethod2IL.Emit(OpCodes.Pop);

                }


                ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
                ConfigureMethod2IL.Emit(OpCodes.Ldstr, attributeSchemaName); //Constant

                ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.EntityTypeBuilderPropertyMethod);

                if (options.RequiredSupport && (attributeDefinition.Value.SelectToken("$.isRequired")?.ToObject<bool>() ?? false))
                {
                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4_1);
                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.IsRequiredMethod);
                }

               

                if (attributeDefinition.Value.SelectToken("$.isRowVersion")?.ToObject<bool>() ?? false)
                {
                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.IsRowVersionMethod);
                }
                 

                if (attributeDefinition.Value.SelectToken("$.type.type")?.ToObject<string>().ToLower() == "choice")
                {
                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.HasConversionMethod.MakeGenericMethod(typeof(int)));
                }



                if (attributeDefinition.Value.SelectToken("$.type.type")?.ToObject<string>().ToLower() == "decimal")
                {

                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4, attributeDefinition.Value.SelectToken("$.type.type.sql.precision")?.ToObject<int>() ?? 18);
                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4, attributeDefinition.Value.SelectToken("$.type.type.sql.scale")?.ToObject<int>() ?? 4);
                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.HasPrecisionMethod);
                }

                ConfigureMethod2IL.Emit(OpCodes.Pop);



            }

            ////alternativ keys
            //var keys = entityDefinition.SelectToken("$.keys") as JObject;
            //if (keys != null && !isTablePerTypeChild)
            //{
            //    foreach(var key in keys.OfType<JProperty>())
            //    {
            //        var props = key.Value.ToObject<string[]>();

            //        ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
            //        ConfigureMethod2IL.Emit(OpCodes.Ldc_I4,props.Length); // Array length
            //        ConfigureMethod2IL.Emit(OpCodes.Newarr, typeof(string));
            //        for(var j = 0;j<props.Length;j++)
            //        {
            //            var attributeDefinition = entityDefinition.SelectToken($"$.attributes['{props[j]}']");
            //            var attributeSchemaName = attributeDefinition.SelectToken("$.schemaName")?.ToString();
            //            ConfigureMethod2IL.Emit(OpCodes.Dup);
            //            ConfigureMethod2IL.Emit(OpCodes.Ldc_I4, j);
            //            ConfigureMethod2IL.Emit(OpCodes.Ldstr, attributeSchemaName);
            //            ConfigureMethod2IL.Emit(OpCodes.Stelem_Ref);
            //        }

                   
            //        ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.EntityTypeBuilderHasAlternateKey);
            //        ConfigureMethod2IL.Emit(OpCodes.Pop);
            //    }
            //}


            ConfigureMethod2IL.Emit(OpCodes.Ret);

            options.EntityDTOConfigurations[entityCollectionSchemaName] = entityTypeConfiguration.CreateTypeInfo();

        }


        public void CreateDTO(ModuleBuilder myModule, string entityCollectionSchemaName, string entitySchameName, JProperty entityDefinition2, JToken manifest)
        {
            // var members = new Dictionary<string, PropertyBuilder>();






            var entityLogicalName = entityDefinition2.Value.SelectToken("$.logicalName").ToString();

            // TypeBuilder entityType =

            // options.EntityDTOsBuilders[entityCollectionSchemaName] = entityType;

            (TypeBuilder entityType, Type baseType) = GetOrCreateEntityBuilder(myModule, entitySchameName, manifest, entityDefinition2.Value as JObject);

            //            if (!options.GeneratePoco)
            {
                SetEntityAttribute(entityDefinition2, entityType);

                SetEntityDTOAttribute(entityDefinition2, entityType);

            }
            //  var propertyChangedMethod = options.EntityBaseClass.GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var attributeDefinition in entityDefinition2.Value.SelectToken("$.attributes").OfType<JProperty>())
            {
                var attributeSchemaName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");


                var clrType = GetCLRType(myModule,entityDefinition2, attributeDefinition.Value,manifest, out var manifestType);


                




                var isprimaryKey = attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;


                if (baseType.GetProperties().Any(p => p.Name == attributeSchemaName))
                {
                    continue;
                }




                //PrimaryKeys cant be null, remove nullable
                if (isprimaryKey)
                {
                    clrType = Nullable.GetUnderlyingType(clrType) ?? clrType;
                }

                if (clrType != null)
                {
                    if (attributeSchemaName=="Loadguaranteeprovided")
                    {

                    }
                    var (attProp, attField) = CreateProperty(entityType, attributeSchemaName, clrType);


                    if (manifestType == "lookup")
                    {
                        var FKLogicalName = attributeDefinition.Value.SelectToken("$.logicalName").ToString();

                        if (FKLogicalName.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                            FKLogicalName = FKLogicalName.Substring(0, FKLogicalName.Length - 2);

                        var FKSchemaName = attributeDefinition.Value.SelectToken("$.schemaName").ToString();
                        if (FKSchemaName.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                            FKSchemaName = FKSchemaName.Substring(0, FKSchemaName.Length - 2);


                        var foreigh = manifest.SelectToken($"$.entities['{attributeDefinition.Value.SelectToken("$.type.referenceType").ToString()}']") as JObject;
                        //  name= foreigh.SelectToken("$.pluralName")?.ToString()
                        var foreighSchemaName = foreigh.SelectToken("$.schemaName")?.ToString();
                       
                        //  var foreighEntityCollectionSchemaName = (foreigh.SelectToken("$.pluralName")?.ToString() ?? (foreigh.Parent as JProperty).Name).Replace(" ", "");

                        try
                        {
                            var (attFKProp, attFKField) = CreateProperty(entityType, (FKSchemaName ??
                                (foreigh.Parent as JProperty).Name).Replace(" ", ""), foreighSchemaName == entitySchameName ?
                                    entityType :
                                    GetRemoteTypeIfExist(foreigh) ??
                                    GetOrCreateEntityBuilder(myModule, foreighSchemaName, manifest, foreigh).Item1 as Type);


                            CustomAttributeBuilder ForeignKeyAttributeBuilder = new CustomAttributeBuilder(options.ForeignKeyAttributeCtor, new object[] { attProp.Name });

                            attFKProp.SetCustomAttribute(ForeignKeyAttributeBuilder);

                            CreateJsonSerializationAttribute(attributeDefinition, attFKProp, FKLogicalName);
                            CreateDataMemberAttribute(attFKProp, FKLogicalName);

                        }
                        catch (Exception ex)
                        {
                            File.AppendAllLines("err.txt", new[] { $"Faiiled for {entitySchameName}.{attributeSchemaName} with {foreighSchemaName}" });

                            throw;
                        }

                    }





                    options.OnDTOTypeGeneration?.Invoke(attributeDefinition.Value, attProp);

                    CreateDataMemberAttribute( attProp, attributeDefinition.Value.SelectToken("$.logicalName").ToString());

                    CreateJsonSerializationAttribute(attributeDefinition, attProp);
                    
                    var isprimaryField = attributeDefinition.Value.SelectToken("$.isPrimaryField")?.ToObject<bool>() ?? false;

                    if (isprimaryField)
                    {
                        CustomAttributeBuilder PrimaryFieldAttributeBuilder = new CustomAttributeBuilder(PrimaryFieldAttributeCtor,new object[] { });

                        attProp.SetCustomAttribute(PrimaryFieldAttributeBuilder);
                    }

                    //CustomAttributeBuilder MetadataAttributeBuilder = new CustomAttributeBuilder(MetadataAttributeCtor, new object[] { }, new[] { MetadataAttributeSchemaNameProperty }, new[] { attributeSchemaName });

                    //attProp.SetCustomAttribute(MetadataAttributeBuilder);

                    //ConfigureMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                    //ConfigureMethodIL.Emit(OpCodes.Ldstr, attProp.Name); //Constant

                    //ConfigureMethodIL.Emit(OpCodes.Callvirt, propertyMethod);

                    //ConfigureMethodIL.Emit(OpCodes.Pop);


                }

            }

            foreach (var entity in manifest.SelectToken("$.entities").OfType<JProperty>())
            {
                var attributes = entity.Value.SelectToken("$.attributes").OfType<JProperty>()
                    .Where(attribute =>
                    {
                        var type = attribute.Value.SelectToken("$.type.referenceType")?.ToString();
                        return type != null && manifest.SelectToken($"$.entities['{type}'].logicalName")?.ToString() == entityDefinition2.Value.SelectToken("$.logicalName").ToString();

                    }).ToArray();
                foreach (var attribute in attributes)
                {
                    {
                      //  File.AppendAllLines("test1.txt", new[] { $"{entity.Value.SelectToken("$.collectionSchemaName")?.ToString()} in {string.Join(",", options.EntityDTOsBuilders.Keys)}" });


                        try
                        {
                            TypeBuilder related = GetOrCreateEntityBuilder(myModule, entity.Name.Replace(" ", ""), manifest, entity.Value as JObject).Item1;

                            //  if (options.EntityDTOsBuilders.ContainsKey(entity.Value.SelectToken("$.collectionSchemaName")?.ToString()))
                            {
                                //
                                var (attProp, attField) = CreateProperty(entityType, (attributes.Length > 1 ? attribute.Name.Replace(" ", "") : "") + entity.Value.SelectToken("$.collectionSchemaName")?.ToString(), typeof(ICollection<>).MakeGenericType(related));
                                // methodAttributes: MethodAttributes.Virtual| MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig);

                                CustomAttributeBuilder ForeignKeyAttributeBuilder = new CustomAttributeBuilder(options.InverseAttributeCtor, new object[] { attribute.Name.Replace(" ", "") });

                                attProp.SetCustomAttribute(ForeignKeyAttributeBuilder);
                               
                                CreateJsonSerializationAttribute(attribute, attProp, attProp.Name.ToLower());
                             //   CreateDataMemberAttribute(attProp, attProp.Name.ToLower());

                            }
                        }catch(Exception ex)
                        {
                           // File.AppendAllLines("test1.txt", new[] {$"failed to create lookup property {entity.Name.Replace(" ", "")}.{attribute.Name.Replace(" ", "")} for {entityDefinition2.Value.SelectToken("$.logicalName").ToString()}" , ex.ToString() });

                            throw;
                        }
                    }
                }
            }

            // ConfigureMethodIL.Emit(OpCodes.Ret);


            // options.EntityDTOs[entityCollectionSchemaName] = options.DTOAssembly?.GetTypes().FirstOrDefault(t => t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute attr && attr.LogicalName == entityLogicalName)?.GetTypeInfo() ?? entityType.CreateTypeInfo();










        }

        private static void SetEntityAttribute(JProperty entityDefinition, TypeBuilder entityType)
        {
            entityType.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] {
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) ,
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.SchemaName)),
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.CollectionSchemaName)),
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.IsBaseClass))
                        
                    }, new object[] {
                    entityDefinition.Value.SelectToken("$.logicalName").ToString() ,
                     entityDefinition.Value.SelectToken("$.schemaName").ToString(),
                      entityDefinition.Value.SelectToken("$.collectionSchemaName").ToString(),
                      entityDefinition.Value.SelectToken("$.abstract")?.ToObject<bool>()??false
                    }));
        }

        protected virtual void SetEntityDTOAttribute(JProperty entityDefinition, TypeBuilder entityType)
        {
            var schema = entityDefinition.Value.SelectToken("$.schema")?.ToString() ?? options.Schema ?? "dbo";

            entityType.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityDTOAttribute).GetConstructor(new Type[] { }),
                new object[] { },
                new[] {
                    typeof(EntityDTOAttribute).GetProperty(nameof(EntityDTOAttribute.LogicalName)),
                    typeof(EntityDTOAttribute).GetProperty(nameof(EntityDTOAttribute.Schema))
                },
                new[] { entityDefinition.Value.SelectToken("$.logicalName").ToString() ,
                    schema
                }));
        }

        private TypeInfo GetRemoteTypeIfExist(JToken entity)
        {
            var schema = entity.SelectToken("$.schema")?.ToString() ?? options.Schema ?? "dbo";

            //if (options.UseOnlyExpliciteExternalDTOClases)
            //{
            //    entity.SelectToken("$.external")
            //}
          
            //var _new = entity.SelectToken("$.new")?.ToObject<bool>() ?? false;
            string foreighentityLogicalName = entity.SelectToken("$.logicalName")?.ToString();

            var result= options.DTOAssembly?.GetTypes().FirstOrDefault(t => t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute attr && attr.LogicalName == foreighentityLogicalName && (options.SkipValidateSchemaNameForRemoteTypes || string.Equals( attr.Schema , schema,StringComparison.OrdinalIgnoreCase)))?.GetTypeInfo();
            return result;
        }

        private bool CompairProps(Type c, string[] allProps)
        {
            var allPropsFromType = GetProperties(c).ToArray();
          //  File.AppendAllLines("test1.txt", new[] { $"Compare for {c.Name}: {string.Join(",", allPropsFromType)}|{string.Join(",", allProps)}" });
            return allPropsFromType.All(p => allProps.Contains(p));
        }

        private IEnumerable<string> GetProperties(Type c)
        {
            var fk = c.GetProperties().Where(p => p.GetCustomAttribute(options.ForeignKeyAttributeCtor.DeclaringType) == null)
                .Select(p => p.Name)
                .ToList();
            return fk;
            return c.GetProperties().Select(p => p.Name).Where(p => !fk.Any(fkp => string.Equals(fkp + "id", p, StringComparison.OrdinalIgnoreCase)));
        }

        private (TypeBuilder, Type) GetOrCreateEntityBuilder(ModuleBuilder myModule, string entitySchameName, JToken manifest, JObject entityDefinition)
        {
            var allProps = entityDefinition.SelectToken("$.attributes").OfType<JProperty>().Select(attributeDefinition => attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "")).ToArray();

            var acceptableBasesClass = options.DTOBaseClasses.Concat(new[] { typeof(DynamicEntity) }).Where(c => CompairProps(c, allProps))
              .OrderByDescending(c => c.GetProperties().Length)
              .First();


            var tpt = entityDefinition.SelectToken($"$.TPT")?.ToString(); ;
            if (!string.IsNullOrEmpty(tpt))
            {

                acceptableBasesClass = options.EntityDTOsBuilders[manifest.SelectToken($"$.entities['{tpt}'].schemaName").ToString()].CreateTypeInfo();
            }


            if (options.GeneratePoco)
            {
                if (acceptableBasesClass == typeof(DynamicEntity))
                    acceptableBasesClass = typeof(object);
            }


         //   File.AppendAllLines("test1.txt", new[] { $"Creating Entity Type for {options.Namespace}.{entitySchameName}" });

            return (options.EntityDTOsBuilders.GetOrAdd(entitySchameName, _ => myModule.DefineType($"{options.Namespace}.{_}",TypeAttributes.Public
                                                                        | (entityDefinition.SelectToken("$.abstract")?.ToObject<bool>() ?? false ? TypeAttributes.Class : TypeAttributes.Class)
                                                                        | TypeAttributes.AutoClass
                                                                        | TypeAttributes.AnsiClass
                                                                        | TypeAttributes.Serializable
                                                                        | TypeAttributes.BeforeFieldInit, acceptableBasesClass)), acceptableBasesClass);
        }

        private void CreateJsonSerializationAttribute(JProperty attributeDefinition, PropertyBuilder attProp, string name=null)
        {
            name = name ?? attributeDefinition.Value.SelectToken("$.logicalName").ToString();
            //[Newtonsoft.Json.JsonConverter(typeof(ChoicesConverter),typeof(AllowedGrantType), "allowedgranttype")]
            if (!options.GeneratePoco)
            {
                CustomAttributeBuilder JsonPropertyAttributeBuilder = new CustomAttributeBuilder(options.JsonPropertyAttributeCtor, new object[] { name });
                CustomAttributeBuilder JsonPropertyNameAttributeBuilder = new CustomAttributeBuilder(options.JsonPropertyNameAttributeCtor, new object[] { name });


                attProp.SetCustomAttribute(JsonPropertyAttributeBuilder);
                attProp.SetCustomAttribute(JsonPropertyNameAttributeBuilder);

                //if(attributeDefinition.Value.SelectToken("$.type.type")?.ToString().ToLower() == "choices")
                //{
                //    CustomAttributeBuilder JsonConverterAttributeBuideer = new CustomAttributeBuilder(options.JsonConverterAttributeCtor, new object[] { options.ChoiceConverter, new object[] {  });

                //}
            }
        }

        static ConstructorInfo DataMemberAttributeCtor = typeof(DataMemberAttribute).GetConstructor(new Type[] { });
        static ConstructorInfo MetadataAttributeCtor = typeof(AttributeAttribute).GetConstructor(new Type[] { });
        static ConstructorInfo PrimaryFieldAttributeCtor = typeof(PrimaryFieldAttribute).GetConstructor(new Type[] { });
        static PropertyInfo DataMemberAttributeNameProperty = typeof(DataMemberAttribute).GetProperty("Name");
        static PropertyInfo MetadataAttributeSchemaNameProperty = typeof(AttributeAttribute).GetProperty("SchemaName");


        public virtual void CreateDataMemberAttribute(PropertyBuilder attProp, string name)
        {

            CustomAttributeBuilder DataMemberAttributeBuilder = new CustomAttributeBuilder(DataMemberAttributeCtor, new object[] { }, new[] { DataMemberAttributeNameProperty }, new[] { name });

            attProp.SetCustomAttribute(DataMemberAttributeBuilder);


        }

        private static bool IsPendingTypeBuilder(Type foo)
        {
            return (foo is TypeBuilder) && !((foo as TypeBuilder).IsCreated());
        }

        private (Type, ConstructorBuilder, Dictionary<string, PropertyBuilder>) CreateColumnsType(JToken manifest, string entitySchameName, string entityCollectionSchemaName, JObject entityDefinition, ModuleBuilder builder)
        {
            var logicalName = entityDefinition.SelectToken("$.logicalName").ToString();
           
            var members = new Dictionary<string, PropertyBuilder>();
            TypeBuilder entityType =
                builder.DefineType($"{options.Namespace}.{entitySchameName}Columns_{options.migrationName.Replace(".", "_")}", TypeAttributes.Public);


       


            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { entityDefinition.SelectToken("$.logicalName").ToString() });
            entityType.SetCustomAttribute(EntityAttributeBuilder);



            var dfc = entityType.DefineDefaultConstructor(MethodAttributes.Public);

            ConstructorBuilder entityCtorBuilder =
                  entityType.DefineConstructor(MethodAttributes.Public,
                                     CallingConventions.Standard, new[] { options.ColumnsBuilderType });



            ILGenerator entityCtorBuilderIL = entityCtorBuilder.GetILGenerator();

            entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityCtorBuilderIL.Emit(OpCodes.Call, dfc);



            foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
            {
                var attributeLogicalName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString();
                var hasPriorEntity = builder.GetTypes().FirstOrDefault(t => !IsPendingTypeBuilder(t) && t.GetCustomAttributes<EntityMigrationColumnsAttribute>().Any(migrationattribute => migrationattribute.LogicalName == logicalName && migrationattribute.AttributeLogicalName == attributeLogicalName));



                if (hasPriorEntity != null)
                    continue;

                var attributeSchemaName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");

                if (options.PartOfMigration)
                {
                    CustomAttributeBuilder EntityMigrationColumnsAttributeBuilder = new CustomAttributeBuilder(typeof(EntityMigrationColumnsAttribute)
                        .GetConstructor(new Type[] { }), new object[] { }, new[] {
                        typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.LogicalName)),
                        typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.MigrationName)),
                         typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.AttributeLogicalName))
                        },
                            new[] { entityDefinition.SelectToken("$.logicalName").ToString(), options.migrationName, attributeLogicalName });
                    entityType.SetCustomAttribute(EntityMigrationColumnsAttributeBuilder);
                }

               var (typeObj,type) = GetTypeInfo(manifest, attributeDefinition);

                var method = GetColumnForType(options.ColumnsBuilderColumnMethod,type);
                if (method == null)
                    continue;

                entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
                entityCtorBuilderIL.Emit(OpCodes.Ldarg_1);

                var (attProp, attField) = CreateProperty(entityType, attributeSchemaName, options.OperationBuilderAddColumnOptionType);

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


                BuildParametersForcolumn(entityCtorBuilderIL, attributeDefinition, typeObj, type, method);

                entityCtorBuilderIL.Emit(OpCodes.Callvirt, method);
                entityCtorBuilderIL.Emit(OpCodes.Call, attProp.SetMethod);

                members[attributeDefinition.Name] = attProp;

                //  entityCtorBuilderIL

            }
            entityCtorBuilderIL.Emit(OpCodes.Ret);

            var entityClrType = entityType.CreateTypeInfo();
            return (entityClrType, entityCtorBuilder, members);
        }

        private static (JToken,string) GetTypeInfo(JToken manifest, JProperty attributeDefinition)
        {
 
            var typeObj = attributeDefinition.Value.SelectToken("$.type");
            var type = typeObj.ToString().ToLower();
            if (typeObj.Type == JTokenType.Object)
            {
                type = typeObj.SelectToken("$.type").ToString()?.ToLower();
                //type["type"] = type["dbtype"];

                if (type == "lookup")
                {
                    var fatAttributes = manifest.SelectToken($"$.entities['{typeObj.SelectToken("$.referenceType").ToString()}'].attributes");
                    var fat = fatAttributes.OfType<JProperty>().Where(c => c.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)
                        .Select(a => a.Value.SelectToken("$.type")).Single();

                    type = fat.ToString().ToLower();
                    if (fat.Type == JTokenType.Object)
                    {
                        type = fat.SelectToken("$.type").ToString().ToLower();
                    }

                    // attributeSchemaName = attributeSchemaName + "Id";
                }
            }

            return (typeObj, type);
        }

        private void BuildParametersForcolumn(ILGenerator entityCtorBuilderIL, JProperty attributeDefinition, JToken typeObj, string type, MethodInfo method, string tableName=null,string schema=null)
        {
            foreach (var arg1 in method.GetParameters())
            {

                var argName = arg1.Name;
                if (argName == "name")
                    argName = "columnName";


                switch (argName)
                {
                    case "comment" when typeObj.Type == JTokenType.Object && typeObj["description"] is JToken comment:
                        entityCtorBuilderIL.Emit(OpCodes.Ldstr, comment.ToString());
                        continue;
                }

                if (typeObj.Type == JTokenType.Object && typeObj.SelectToken($"$.sql.{argName}") is JToken sqlColumnArgs)
                {
                    if (sqlColumnArgs.Type == JTokenType.String)
                    {
                        entityCtorBuilderIL.Emit(OpCodes.Ldstr, sqlColumnArgs.ToString());
                    }
                    else if (sqlColumnArgs.Type == JTokenType.Integer)
                    {
                        entityCtorBuilderIL.Emit(OpCodes.Ldc_I4, sqlColumnArgs.ToObject<int>());
                        if (Nullable.GetUnderlyingType(arg1.ParameterType) != null)
                        {
                            entityCtorBuilderIL.Emit(OpCodes.Newobj, arg1.ParameterType.GetConstructor(new[] { Nullable.GetUnderlyingType(arg1.ParameterType) }));
                            // It's nullable
                        }
                    }
                    else if (sqlColumnArgs.Type == JTokenType.Boolean)
                    {
                        entityCtorBuilderIL.Emit(sqlColumnArgs.ToObject<bool>() ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        if (Nullable.GetUnderlyingType(arg1.ParameterType) != null)
                        {
                            entityCtorBuilderIL.Emit(OpCodes.Newobj, arg1.ParameterType.GetConstructor(new[] { Nullable.GetUnderlyingType(arg1.ParameterType) }));
                            // It's nullable
                        }

                    }
                    else
                    {
                        entityCtorBuilderIL.Emit(OpCodes.Ldnull);
                    }


                }
                else
                {
                    var hasMaxLength = (typeObj?.SelectToken("$.sql.maxLength")??typeObj?.SelectToken("$.maxLength")) is JToken;

                    switch (argName)
                    {
                        case "maxLength" when typeObj?.SelectToken("$.maxLength") is JToken maxLength: 
                            
                            entityCtorBuilderIL.Emit(OpCodes.Ldc_I4, maxLength.ToObject<int>());
                            if (Nullable.GetUnderlyingType(arg1.ParameterType) != null)
                            {
                                entityCtorBuilderIL.Emit(OpCodes.Newobj, arg1.ParameterType.GetConstructor(new[] { Nullable.GetUnderlyingType(arg1.ParameterType) }));
                                // It's nullable
                            }

                            break;
                        case "table" when !string.IsNullOrEmpty(tableName): entityCtorBuilderIL.Emit(OpCodes.Ldstr,tableName); break;
                        case "schema" when !string.IsNullOrEmpty(schema): entityCtorBuilderIL.Emit(OpCodes.Ldstr, schema); break;
                        case "columnName": entityCtorBuilderIL.Emit(OpCodes.Ldstr, attributeDefinition.Value.SelectToken("$.schemaName").ToString()); break;
                        case "nullable" when ((attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)):
                        case "nullable" when (options.RequiredSupport && (attributeDefinition.Value.SelectToken("$.isRequired")?.ToObject<bool>() ?? false)):
                        case "nullable" when (options.RequiredSupport && (attributeDefinition.Value.SelectToken("$.type.required")?.ToObject<bool>() ?? false)):
                        case "nullable" when ((attributeDefinition.Value.SelectToken("$.isRowVersion")?.ToObject<bool>() ?? false)):
                            //  var a = ((attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)) ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1;
                            entityCtorBuilderIL.Emit(OpCodes.Ldc_I4_0);
                            break;
                        case "nullable":
                            entityCtorBuilderIL.Emit(OpCodes.Ldc_I4_1);
                            break;
                        case "type" when type == "multilinetext":
                            entityCtorBuilderIL.Emit(OpCodes.Ldstr, "nvarchar(max)");
                            break;

                        case "type" when type == "text" && !hasMaxLength:
                        case "type" when type == "string" && !hasMaxLength:
                            entityCtorBuilderIL.Emit(OpCodes.Ldstr, $"nvarchar({((attributeDefinition.Value.SelectToken("$.isPrimaryField")?.ToObject<bool>() ?? false) ? 255 : 100)})");
                            break;
                        case "rowVersion" when ((attributeDefinition.Value.SelectToken("$.isRowVersion")?.ToObject<bool>() ?? false)):
                            entityCtorBuilderIL.Emit(OpCodes.Ldc_I4_1);
                            break;
                        default:

                            entityCtorBuilderIL.Emit(OpCodes.Ldnull);
                            break;
                    }


                }
                //
                //new ColumnsBuilder(null).Column<Guid>(
                //    type:"",unicode:false,maxLength:0,rowVersion:false,name:"a",nullable:false,defaultValue:null,defaultValueSql:null,
                //    computedColumnSql:null, fixedLength:false,comment:"",collation:"",precision:0,scale:0,stored:false);
                //type:

            }
        }

        private MethodInfo GetColumnForType(MethodInfo method, string manifestType)
        {

            

            var type = GetCLRType(manifestType);
            if (type != null)
                return method.MakeGenericMethod(type);

            //switch (manifestType.ToLower())
            //{
            //    case "string":
            //    case "text":
            //    case "multilinetext":
            //        return baseMethodType.MakeGenericMethod(typeof(string));
            //    case "guid":
            //        return baseMethodType.MakeGenericMethod(typeof(Guid));
            //    case "int":
            //    case "integer":
            //        return baseMethodType.MakeGenericMethod(typeof(int));
            //    case "datetime":
            //        return baseMethodType.MakeGenericMethod(typeof(DateTime));
            //    case "decimal":
            //        return baseMethodType.MakeGenericMethod(typeof(decimal));
            //    case "boolean":
            //        return baseMethodType.MakeGenericMethod(typeof(bool));

            //}
            return null;
            //   return baseMethodType;
        }

        public static (PropertyBuilder, FieldBuilder) CreateProperty(TypeBuilder entityTypeBuilder, string name, Type type, PropertyAttributes props = PropertyAttributes.None,
            MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig)
        {
            var proertyBuilder = entityTypeBuilder.DefineProperty(name, props, type, Type.EmptyTypes);

            FieldBuilder customerNameBldr = entityTypeBuilder.DefineField($"_{name}",
                                                       type,
                                                       FieldAttributes.Private);

            // The property set and property get methods require a special
            // set of attributes.




            // Define the "get" accessor method for CustomerName.
            MethodBuilder custNameGetPropMthdBldr =
                entityTypeBuilder.DefineMethod($"get_{name}",
                                           methodAttributes,
                                           type,
                                           Type.EmptyTypes);

            ILGenerator custNameGetIL = custNameGetPropMthdBldr.GetILGenerator();

            custNameGetIL.Emit(OpCodes.Ldarg_0);
            custNameGetIL.Emit(OpCodes.Ldfld, customerNameBldr);
            custNameGetIL.Emit(OpCodes.Ret);

            // Define the "set" accessor method for CustomerName.
            MethodBuilder custNameSetPropMthdBldr =
                entityTypeBuilder.DefineMethod($"set_{name}",
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

            return (proertyBuilder, customerNameBldr);
        }
    }
}
