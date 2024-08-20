using EAVFW.Extensions.Manifest.SDK;
using EAVFW.Extensions.Manifest.SDK.DTO;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

namespace EAVFramework.Shared.V2
{

    public class MigrationBuilderBuilder
    {
        private readonly DynamicAssemblyBuilder builder;
        private MethodBuilder upMethod;
        private readonly DynamicCodeService dynamicCodeService;
        private CodeGenerationOptions options;
        private Lazy<ILGenerator> il;

        public ILGenerator UpMethodIL => this.il.Value;

        public MigrationBuilderBuilder(DynamicAssemblyBuilder builder, MethodBuilder upMethod, DynamicCodeService dynamicCodeService, CodeGenerationOptions options)
        {
            this.builder = builder;
            this.upMethod = upMethod;
            this.dynamicCodeService = dynamicCodeService;
            this.options = options;

            this.il = new Lazy<ILGenerator>(() => upMethod.GetILGenerator());
        }


        public void DropForeignKey(string table, string schema, string fkname)
        {
            UpMethodIL.Emit(OpCodes.Ldarg_1);

            foreach (var arg1 in options.MigrationsBuilderDropForeignKey.GetParameters())
            {
                var argName = arg1.Name;


                switch (argName)
                {
                    case "table" when !string.IsNullOrEmpty(table): UpMethodIL.Emit(OpCodes.Ldstr, table); break;
                    case "schema" when !string.IsNullOrEmpty(schema): dynamicCodeService.EmitPropertyService.EmitNullable(UpMethodIL, () => UpMethodIL.Emit(OpCodes.Ldstr, schema), arg1); break;
                    case "name": UpMethodIL.Emit(OpCodes.Ldstr, fkname); break;
                }
            }
            UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationsBuilderDropForeignKey);
            UpMethodIL.Emit(OpCodes.Pop);
        }

        public bool EmitAddColumn(string table, string schema, DynamicPropertyBuilder attributeDefinition)
        {
            var method = options.MigrationsBuilderAddColumn.MakeGenericMethod(attributeDefinition.PropertyType);


            UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                                              //MigrationsBuilderAddColumn

            var required = attributeDefinition.IsRequired;
            if (required)
                attributeDefinition.Required(false);
            BuildParametersForcolumn(UpMethodIL, attributeDefinition, method, table, schema);
            attributeDefinition.Required(required);
            UpMethodIL.Emit(OpCodes.Callvirt, method);
            UpMethodIL.Emit(OpCodes.Pop);
            return required;
        }

        

        public void EmitAlterColumn(string table, string schema, DynamicPropertyBuilder attributeDefinition)
        {
            var method = options.MigrationsBuilderAlterColumn.MakeGenericMethod(attributeDefinition.PropertyType);


            UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                                              //MigrationsBuilderAddColumn

            BuildParametersForcolumn(UpMethodIL, attributeDefinition, method, table, schema);

            UpMethodIL.Emit(OpCodes.Callvirt, method);
            UpMethodIL.Emit(OpCodes.Pop);
        }


       

        public (Type, ConstructorBuilder, Dictionary<string, PropertyBuilder>) CreateColumnsType(string schemaName,string logicalName, 
             string migrationName, bool partOfMigration, IReadOnlyCollection< DynamicPropertyBuilder> props)
        {
            
            var options = dynamicCodeService.Options;

            var members = new Dictionary<string, PropertyBuilder>();

            var columnsType = builder.Module.DefineType($"{builder.Namespace}.{schemaName}Columns_{migrationName.Replace(".", "_")}", TypeAttributes.Public);




            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { logicalName });
            columnsType.SetCustomAttribute(EntityAttributeBuilder);



            var dfc = columnsType.DefineDefaultConstructor(MethodAttributes.Public);

            ConstructorBuilder entityCtorBuilder =
                  columnsType.DefineConstructor(MethodAttributes.Public,
                                     CallingConventions.Standard, new[] { options.ColumnsBuilderType });



            ILGenerator entityCtorBuilderIL = entityCtorBuilder.GetILGenerator();

            entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityCtorBuilderIL.Emit(OpCodes.Call, dfc);


           
            foreach (var propertyInfo in props)
            {
                var attributeLogicalName = propertyInfo.LogicalName;
                var attributeSchemaName = propertyInfo.SchemaName;

                if (propertyInfo.PropertyType == null)
                    continue;

                var method = options.ColumnsBuilderColumnMethod.MakeGenericMethod(propertyInfo.PropertyType);


                entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
                entityCtorBuilderIL.Emit(OpCodes.Ldarg_1);

                var (attProp, attField) = dynamicCodeService.EmitPropertyService.CreateProperty(columnsType, attributeSchemaName, options.OperationBuilderAddColumnOptionType); //CreateProperty(entityType, attributeSchemaName, options.OperationBuilderAddColumnOptionType);



                var columparams = BuildParametersForcolumn(entityCtorBuilderIL, propertyInfo, method);

                entityCtorBuilderIL.Emit(OpCodes.Callvirt, method);
                entityCtorBuilderIL.Emit(OpCodes.Call, attProp.SetMethod);


                if (partOfMigration)
                {
                    var attributeProperties = columparams.Keys.Concat(new[] {
                            typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.LogicalName)),
                            typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.MigrationName)),
                            typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.AttributeLogicalName)),
                            typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.AttributeHash)),
                             typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.AttributeTypeHash))
                        }).ToArray();
                    var attributesValues = columparams.Values.Concat(new[] {
                               logicalName,
                                migrationName,
                                attributeLogicalName,
                                propertyInfo.ExternalHash,
                               propertyInfo.ExternalTypeHash
                    }).ToArray();

                    CustomAttributeBuilder EntityMigrationColumnsAttributeBuilder = new CustomAttributeBuilder(typeof(EntityMigrationColumnsAttribute)
                        .GetConstructor(new Type[] { }),
                        new object[] { }, attributeProperties, attributesValues);

                    columnsType.SetCustomAttribute(EntityMigrationColumnsAttributeBuilder);
                }


                members[attributeLogicalName] = attProp;


            }
            entityCtorBuilderIL.Emit(OpCodes.Ret);

            var entityClrType = columnsType.CreateTypeInfo();
            return (entityClrType, entityCtorBuilder, members);
        }

        private Dictionary<PropertyInfo, object> BuildParametersForcolumn(ILGenerator entityCtorBuilderIL, DynamicPropertyBuilder propertyInfo, MethodInfo method, string tableName = null, string schema = null)
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


                var value = propertyInfo.GetColumnParam(argName);
                if (value != null)
                {
                    switch (value)
                    {
                        case string stringvalue:
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, stringvalue), arg1);

                            break;
                        case int intvalue:
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldc_I4, intvalue), arg1);

                            break;

                        case bool boolvalue:
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(boolvalue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0), arg1);
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


                        case "maxLength" when propertyInfo.MaxLength.HasValue:

                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldc_I4, propertyInfo.MaxLength.Value), arg1);

                            AddParameterComparison(parameters, argName, propertyInfo.MaxLength.Value);

                            break;
                        case "table" when !string.IsNullOrEmpty(tableName): entityCtorBuilderIL.Emit(OpCodes.Ldstr, tableName); break;
                        case "schema" when !string.IsNullOrEmpty(schema): dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, schema), arg1); break;
                        case "columnName": entityCtorBuilderIL.Emit(OpCodes.Ldstr, propertyInfo.SchemaName); break;
                        case "nullable" when (propertyInfo.IsPrimaryKey):
                        case "nullable" when (options.RequiredSupport && (propertyInfo.IsRequired)):
                        case "nullable" when (propertyInfo.IsRowVersion):
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldc_I4_0), arg1);
                            break;
                        case "nullable":
                            entityCtorBuilderIL.Emit(OpCodes.Ldc_I4_1);
                            break;
                        case "type" when propertyInfo.Type == "mpultilinetext":
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, "nvarchar(max)"), arg1);
                            break;

                        case "type" when propertyInfo.Type == "text" && !propertyInfo.MaxLength.HasValue:
                        case "type" when propertyInfo.Type == "string" && !propertyInfo.MaxLength.HasValue:
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

     

        public void CreateIndex(string table, string schema, string name, bool unique = true, params string[] columns)
        {

            /**
             * <summary>
             *     Builds a <see cref="CreateIndexOperation" /> to create a new index.
             * </summary>
             * <remarks>
             *     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see> for more information and examples.
             * </remarks>
             * <param name="name">The index name.</param>
             * <param name="table">The table that contains the index.</param>
             * <param name="column">The column that is indexed.</param>
             * <param name="schema">The schema that contains the table, or <see langword="null" /> to use the default schema.</param>
             * <param name="unique">Indicates whether or not the index enforces uniqueness.</param>
             * <param name="filter">The filter to apply to the index, or <see langword="null" /> for no filter.</param>
             * <param name="descending">
             *     A set of values indicating whether each corresponding index column has descending sort order.
             *     If <see langword="null" />, all columns will have ascending order.
             *     If an empty array, all columns will have descending order.
             * </param>
             * <returns>A builder to allow annotations to be added to the operation.</returns>
             * public virtual OperationBuilder<CreateIndexOperation> CreateIndex(
             */


            try
            {
                UpMethodIL.Emit(OpCodes.Ldarg_1); //this first argument
                UpMethodIL.Emit(OpCodes.Ldstr, name); //#1 Constant keyname 
                UpMethodIL.Emit(OpCodes.Ldstr, table); //#2 Constant table name


                UpMethodIL.Emit(OpCodes.Ldc_I4, columns.Length); //#3 Array length
                UpMethodIL.Emit(OpCodes.Newarr, typeof(string));
                for (var j = 0; j < columns.Length; j++)
                {

                    UpMethodIL.Emit(OpCodes.Dup);
                    UpMethodIL.Emit(OpCodes.Ldc_I4, j);
                    UpMethodIL.Emit(OpCodes.Ldstr, columns[j]);
                    UpMethodIL.Emit(OpCodes.Stelem_Ref);
                }



                UpMethodIL.Emit(OpCodes.Ldstr, schema); //#4 Constant schema
                UpMethodIL.Emit(unique ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0); //#5Constant unique=true
                UpMethodIL.Emit(OpCodes.Ldnull); //#6Constant filter=null

                /**
                 * In EF7+ an extra parameter was added.
                 * 
                 */
                if (options.MigrationBuilderCreateIndex.GetParameters().Length == 7)
                    UpMethodIL.Emit(OpCodes.Ldnull); //#8Constant order=null


                UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderCreateIndex);
                UpMethodIL.Emit(OpCodes.Pop);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create key for {table} | {name}", ex);
            }
        }
    }

    [DebuggerDisplay("TableBuilder = {SchemaName}")]
    public class DynamicTableBuilder : IDynamicTableBuilder
    {
        protected ConcurrentDictionary<string, DynamicPropertyBuilder> properties = new ConcurrentDictionary<string, DynamicPropertyBuilder>();

        public IReadOnlyCollection<DynamicPropertyBuilder> Properties => properties.Values.Where(p => p.IsManifestType).OrderByDescending(c => c.IsPrimaryKey).ThenByDescending(c => c.IsPrimaryField).ThenBy(c => c.LogicalName).ToArray();

        public IReadOnlyCollection<DynamicPropertyBuilder> AllProperties => Properties.Concat(Parent?.AllProperties ?? Enumerable.Empty<DynamicPropertyBuilder>()).ToArray();

        private DynamicCodeService dynamicCodeService;
        private ModuleBuilder myModule;
        public DynamicAssemblyBuilder DynamicAssemblyBuilder { get; }

        public string SchemaName { get; }
        public TypeBuilder Builder { get; }
        public TypeBuilder ConfigurationBuilder { get; }

        public string CollectionSchemaName { get; }
        public string LogicalName { get; }
        public bool IsBaseEntity { get; }
        public string Schema { get; }

        public string EntityKey { get; }

        public bool IsExternal { get; private set; }
        public TypeInfo RemoteType { get; private set; }

        public DynamicTableBuilder(
            DynamicCodeService dynamicCodeService,
            ModuleBuilder myModule,
            DynamicAssemblyBuilder dynamicAssemblyBuilder,
            string entityKey,
            string tableSchemaName, string tableLogicalName, string collectionSchemaName, string schema = "dbo", bool isAbstract = false, MappingStrategy? mappingStrategy = null)
        {
            EntityKey = entityKey;
            SchemaName = tableSchemaName;
            CollectionSchemaName = collectionSchemaName;
            LogicalName = tableLogicalName;
            Schema = schema;
            IsBaseEntity = isAbstract;
            this.dynamicCodeService = dynamicCodeService;
            this.myModule = myModule;
            this.DynamicAssemblyBuilder = dynamicAssemblyBuilder;
            this.MappingStrategy = mappingStrategy;


            Builder = myModule.DefineType($"{dynamicAssemblyBuilder.Namespace}.{SchemaName}", TypeAttributes.Public
                                                                        | (isAbstract && dynamicCodeService.Options.GenerateAbstractClasses ? TypeAttributes.Abstract : TypeAttributes.Class)
                                                                        | TypeAttributes.AutoClass
                                                                        | TypeAttributes.AnsiClass
                                                                        | TypeAttributes.Serializable
                                                                        | TypeAttributes.BeforeFieldInit);
            ConfigurationBuilder = myModule.DefineType($"{DynamicAssemblyBuilder.Namespace}.{SchemaName}Configuration", TypeAttributes.Public
                                                         | TypeAttributes.Class
                                                         | TypeAttributes.AutoClass
                                                         | TypeAttributes.AnsiClass
                                                         | TypeAttributes.Serializable
                                                         | TypeAttributes.BeforeFieldInit, null, new Type[] { dynamicCodeService.Options.EntityConfigurationInterface });



            SetEntityDTOAttribute();
            SetEntityAttribute();

        }


        public ConcurrentBag<Type> Interfaces { get; } = new ConcurrentBag<Type>();
        internal void AddInterface(Type a)
        {
            Interfaces.Add(a);

            this.Builder.AddInterfaceImplementation(a);
        }

        protected virtual void AddInterfaces()
        {
            this.dynamicCodeService.EmitPropertyService.AddInterfaces(this);


        }
        protected virtual void SetEntityAttribute()
        {
            Builder.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] {
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) ,
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.SchemaName)),
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.CollectionSchemaName)),
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.IsBaseClass)),
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.EntityKey))

                    }, new object[] {
                   LogicalName ,
                   SchemaName,
                   CollectionSchemaName,
                   IsBaseEntity,
                   EntityKey
                    }));


        }

        public List<string> SQLUpStatements { get; } = new List<string>();

        protected virtual void SetEntityDTOAttribute()
        {


            Builder.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityDTOAttribute).GetConstructor(new Type[] { }),
                new object[] { },
                new[] {
                    typeof(EntityDTOAttribute).GetProperty(nameof(EntityDTOAttribute.LogicalName)),
                    typeof(EntityDTOAttribute).GetProperty(nameof(EntityDTOAttribute.Schema))
                },
                new[] {
                   LogicalName,
                    Schema
                }));
        }

        public void WithBaseEntity(DynamicTableBuilder identity)
        {
            this.Parent = identity;
        }


        public DynamicPropertyBuilder AddProperty(string attributeKey, string propertyName, string logicalName, string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException($"'{nameof(type)}' cannot be null or whitespace for property {attributeKey}", nameof(type));

            }
            // if (ContainsProperty(propertyName))
            //    return null;



            return properties.GetOrAdd(propertyName, (_) => new DynamicPropertyBuilder(dynamicCodeService, this, attributeKey, propertyName, logicalName, type));
        }
        public DynamicPropertyBuilder AddProperty(string attributeKey, string propertyName, string logicalName, Type type)
        {
            //if (ContainsProperty(propertyName))
            //    return null;

            return properties.GetOrAdd(propertyName, (_) => new DynamicPropertyBuilder(dynamicCodeService, this, attributeKey, propertyName, logicalName, type));
        }
        public PropertyInfo GetParentPropertyGetMethod(string prop)
        {
            var test = this;
            while (test != null)
            {
              

                if(test.Parent == null && test.ClrParentType != null)
                {
                    var type = test.ClrParentType;
                    while (type != null)
                    {
                        if (type.GetProperties().Any(p => p.Name == prop))
                        {
                            if (type.IsGenericTypeDefinition)
                            {
                                
                                return type.MakeGenericType(
                                    typeArguments: type.GetGenericArguments().Select(a=>
                                    {
                                        if (a.IsGenericParameter)
                                            return a.GetGenericParameterConstraints().First();
                                        return a;
                                        //throw new Exception($"Not able to build model for {a.Name}, {type.Name}");

                                    }).ToArray())
                                    .GetProperty(prop);
                            }
                            return type.GetProperties().Where(p => p.Name == prop).First();

                        }

                        type = type.BaseType;
                    }
                }

                test = test.Parent;
                
            }
            return null;
        }
        public static bool IsBuilder(Type type) => type is TypeBuilder || type is EnumBuilder || type is GenericTypeParameterBuilder;
        public static bool IsTypeBuilderInstantiation(Type type)
        {
            bool isTypeBuilderInstantiation = false;
            if (type.IsGenericType && !(IsBuilder(type)))
            {
                foreach (var genericTypeArg in type.GetGenericArguments())
                {
                    if (isTypeBuilderInstantiation = (IsBuilder(genericTypeArg) ||
                        IsTypeBuilderInstantiation(genericTypeArg)))
                        break;
                }
                isTypeBuilderInstantiation |= type.GetGenericTypeDefinition() is TypeBuilder;
            }
            return isTypeBuilderInstantiation;
        }
        
        public bool ContainsPropertyFromInterfaceInBaseClass(string propertyName, out Type[] interfaceType, bool build=false)
        {
            interfaceType = Interfaces.Where(i => 
                (!IsTypeBuilderInstantiation (i) && i.GetProperties().Any(p => p.Name == propertyName)) 
                ||i.IsGenericType && i.GetGenericTypeDefinition().GetProperties().Any(p => p.Name == propertyName))
                .Select( t=> build && t.IsGenericType ?
                    t.GetGenericTypeDefinition().MakeGenericType(t.GenericTypeArguments.Select(tt =>
                    {
                        return tt switch
                        {
                            TypeBuilder foo => foo.CreateTypeInfo(),
                            EnumBuilder foo => foo.CreateTypeInfo(),
                            _ => tt
                        };
                    }).ToArray()) : t )
                .ToArray()  ?? Array.Empty<Type>();
            return interfaceType.Any();
          
        }
        //public bool ContainsPropertyFromInterfaceInBaseClass(string propertyName, out Type[] interfaceType)
        //{
        //    interfaceType = Interfaces.Where(i =>
        //        (!IsTypeBuilderInstantiation(i) && i.GetProperties().Any(p => p.Name == propertyName))
        //        || i.IsGenericType && i.GetGenericTypeDefinition().GetProperties().Any(p => p.Name == propertyName))
        //        .Select(t => IsTypeBuilderInstantiation(t) && t.IsGenericType ?
        //            t.GetGenericTypeDefinition().MakeGenericType(t.GenericTypeArguments.Select(c => (c is TypeBuilder foo ? foo.CreateTypeInfo)).ToArray())
        //            : t)
        //        .ToArray() ?? Array.Empty<Type>();
        //    return interfaceType.Any();

        //}
        public bool ContainsParentProperty(string propertyName)
        {
            if ((Parent?.ContainsProperty(propertyName) ?? false))
            {
                return true;
            }

            if (ClrParentType != null)
            {
                if (ClrParentType is TypeBuilder)
                    throw new InvalidOperationException("BaseType cannot be typebuilder");
                try
                {
                    if (ClrParentType.GetProperties().Any(p => p.Name == propertyName))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"BaseType cannot be typebuilder: {ClrParentType.Name}", ex);
                }
            }

            return false;
        }
        public bool ContainsProperty(string propertyName)
        {


            if (properties.ContainsKey(propertyName) || (Parent?.ContainsProperty(propertyName) ?? false))
            {
                return true;
            }

            if (ClrParentType != null)
            {

                if (ClrParentType.GetProperties().Any(p => p.Name == propertyName))
                {
                    return true;
                }
            }



            return false;
        }

        public DynamicTableBuilder WithTable(string entityKey, string tableSchemaName, string tableLogicalName, string tableCollectionSchemaName, string schema, bool isBaseClass, MappingStrategy? mappingStrategy = null)
        {
            return this.DynamicAssemblyBuilder.WithTable(entityKey, tableSchemaName, tableLogicalName, tableCollectionSchemaName, schema, isBaseClass, mappingStrategy);
        }
        public DynamicTableBuilder Parent { get; private set; }

        public string[] AllPropsWithLookups => this.properties.Keys.Concat(this.Parent?.AllPropsWithLookups ?? Enumerable.Empty<string>()).ToArray();
        public TypeInfo GetTypeInfo()
        {
            return Builder.GetTypeInfo();
        }

        public bool IsBuilded { get; private set; }
        public void BuildType()
        {
            if (IsBuilded)
                return;
            IsBuilded = true;
#if DEBUG
            //        File.AppendAllLines("test1.txt", new[] { $"Building {SchemaName}" });
#endif
            BuildParent();


            BuildInverseProperties();

            AddInterfaces();

            foreach (var prop in properties.Values.OrderByDescending(c => c.IsPrimaryKey).ThenByDescending(c => c.IsPrimaryField).ThenBy(c => c.LogicalName))
            {
#if DEBUG
                //       File.AppendAllLines("test1.txt", new[] { $"Building Prop {prop.SchemaName} {PrintTypeName( prop.PropertyType)} {PrintTypeName(prop.DTOPropertyType)}" });
#endif
                prop.Build();
            }



            BuildDTOConfiguration();
#if DEBUG
            //     File.AppendAllLines("test1.txt", new[] { $"Build Complated {SchemaName}" });
#endif
            // BuildMigration();
        }

        private string PrintTypeName(Type propertyType)
        {
            if (propertyType == null)
                return "";

            var underlaying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (underlaying == propertyType)
                return propertyType.Name;
            return underlaying.Name + "?";


        }

        public Type CreateMigrationType(string @namespace, string migrationName, bool partOfMigration)
        {
            var MigrationBuilder = myModule.DefineType($"{DynamicAssemblyBuilder.Namespace}.{CollectionSchemaName}Builder_{migrationName.Replace(".", "_")}", TypeAttributes.Public);
            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { LogicalName });
            MigrationBuilder.SetCustomAttribute(EntityAttributeBuilder);

            var builder = DynamicAssemblyBuilder.Module;
            var options = dynamicCodeService.Options;
            var entityTypeBuilder = MigrationBuilder;

            var upSql = string.Join("\n", SQLUpStatements);


            var priorEntities = dynamicCodeService.GetTypes()
                .Where(t => !t.IsPendingTypeBuilder() && t.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migrationAttribute && migrationAttribute.LogicalName == LogicalName)
                .ToArray();

            if (partOfMigration)
            {

                var EntityMappingStrategyAttributeBuilder = new CustomAttributeBuilder(
                    typeof(EntityMappingStrategyAttribute).GetConstructor(new Type[0]),
                    new object[] { },
                    new[]
                    {
                          typeof(EntityMappingStrategyAttribute).GetProperty(nameof(EntityMappingStrategyAttribute.OldMappingStrategy)),
                          typeof(EntityMappingStrategyAttribute).GetProperty(nameof(EntityMappingStrategyAttribute.MappingStrategy)),
                    }, new object[] {
                        priorEntities.LastOrDefault()?.GetCustomAttribute< EntityMappingStrategyAttribute>().MappingStrategy ?? EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPT,
                        MappingStrategy ?? EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPT });
                entityTypeBuilder.SetCustomAttribute(EntityMappingStrategyAttributeBuilder);

                CustomAttributeBuilder EntityMigrationAttributeBuilder = new CustomAttributeBuilder(
                    typeof(EntityMigrationAttribute).GetConstructor(new Type[] { }),
                    new object[] { },
                    new[] {
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.Namespace)),
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.LogicalName)),
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.MigrationName)) ,
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.RawUpMigration))
                    },


                    new[] { @namespace, LogicalName, migrationName, upSql });
                entityTypeBuilder.SetCustomAttribute(EntityMigrationAttributeBuilder);

                foreach (var k in Keys)
                {
                    entityTypeBuilder.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            typeof(EntityIndexAttribute).GetConstructor(new Type[] { }),
                            new object[] { },
                        new[] {
                     typeof(EntityIndexAttribute).GetProperty(nameof(EntityIndexAttribute.IndexName)),
                        }, new[] {
                            k.Key
                        }));
                }
            }

            entityTypeBuilder.AddInterfaceImplementation(options.DynamicTableType);


            if (IsExternal)
            {
                var UpMethod = entityTypeBuilder.DefineMethod("Up", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderCreateTable.DeclaringType });

                var UpMethodIL = UpMethod.GetILGenerator();

                UpMethodIL.Emit(OpCodes.Ret);

                var DownMethod = entityTypeBuilder.DefineMethod("Down", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderDropTable.DeclaringType });
                var DownMethodIL = DownMethod.GetILGenerator();

                DownMethodIL.Emit(OpCodes.Ret);

                return MigrationBuilder.CreateTypeInfo();
            }

            {

                var UpMethod = entityTypeBuilder.DefineMethod("Up", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderCreateTable.DeclaringType });
                var migrationBuilder = new MigrationBuilderBuilder(DynamicAssemblyBuilder,UpMethod, dynamicCodeService, options);




                bool IsParentTPCStrategry()
                {
                    var c = this; ;


                    while (c != null)
                    {
                        if (c.Parent?.MappingStrategy == EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPC)
                            return true;
                        c = c.Parent;
                    }
                    return false;

                }
                // var name = schemaName;
                var istpc = IsParentTPCStrategry();
               
                var (columnsCLRType, columnsctor, members) = migrationBuilder.CreateColumnsType(this.SchemaName,this.LogicalName,
                    migrationName, partOfMigration, istpc ? this.AllProperties : this.Properties);

                var columsMethod = entityTypeBuilder.DefineMethod("Columns", MethodAttributes.Public, columnsCLRType, new[] { options.ColumnsBuilderType });

                var columsMethodIL = columsMethod.GetILGenerator();
                columsMethodIL.Emit(OpCodes.Ldarg_1);
                columsMethodIL.Emit(OpCodes.Newobj, columnsctor);
                columsMethodIL.Emit(OpCodes.Ret);


                var ConstraintsMethod = entityTypeBuilder.DefineMethod("Constraints",
                    MethodAttributes.Public, null, new[] { options.CreateTableBuilderType.MakeGenericType(columnsCLRType) });
                var ConstraintsMethodIL = ConstraintsMethod.GetILGenerator();


                var primaryKeys = properties.Values.Where(p => p.IsPrimaryKey)
                    .Where(p => columnsCLRType.GetProperty(p.SchemaName) != null) // members.ContainsKey(p.LogicalName))
                    .Select(p => columnsCLRType.GetProperty(p.SchemaName).GetMethod)
                    .ToArray();



                var fKeys = properties.Values.Where(p => p.IsLookup) // entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                                                                     //  .Where(attribute => attribute.Value.SelectToken("$.type.type")?.ToString() == "lookup")
                                                                     // .Where(attribute => members.ContainsKey(attribute.Value.SelectToken("$.logicalName")?.ToString()))
                   .Where(attribute => columnsCLRType.GetProperty(attribute.SchemaName) != null)
                   .Select(attribute => new
                   {
                       Name = $"FK_{CollectionSchemaName}_{attribute.ReferenceType.CollectionSchemaName}_{attribute.SchemaName}".Replace(" ", ""),
                       AttributeSchemaName = attribute.SchemaName,  //attribute.Value.SelectToken("$.schemaName").ToString(),
                       PropertyGetMethod = columnsCLRType.GetProperty(attribute.SchemaName).GetMethod,
                       ReferenceType = attribute.ReferenceType,
                       OnDeleteCascade = attribute.OnDeleteCascade ?? options.ReferentialActionNoAction,
                       OnUpdateCascade = attribute.OnUpdateCascade ?? options.ReferentialActionNoAction,
                       // ForeignKey = attribute.ForeignKey
                   }).OrderBy(n => n.Name)
                   .ToArray();

                if (primaryKeys.Any() || fKeys.Any())
                {
                    ConstraintsMethodIL.DeclareLocal(typeof(ParameterExpression));
                }

                if (primaryKeys.Any())
                {

                    ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, $"PK_{CollectionSchemaName}"); //PK Name

                    dynamicCodeService.EmitPropertyService.WriteLambdaExpression(builder, ConstraintsMethodIL, columnsCLRType, primaryKeys.Select(c => columnsCLRType.GetProperty(c.Name.Substring(4)).GetMethod).ToArray());

                    var createTableMethod = options.CreateTableBuilderType.MakeGenericType(columnsCLRType).GetMethod(options.CreateTableBuilderPrimaryKeyName, BindingFlags.Public | BindingFlags.Instance, null,
                        new[] { typeof(string), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(columnsCLRType, typeof(object))) }, null);
                    ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                    ConstraintsMethodIL.Emit(OpCodes.Pop);
                }


                if (fKeys.Any())
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
                    foreach (var fk in fKeys)
                    {
                        /**
                         * We will skip the FKs if its a TPC and base class. See above comment
                         */
                        if (fk.ReferenceType.IsBaseEntity && fk.ReferenceType.MappingStrategy == EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPC)
                        {
                            continue;
                        }
                        ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, fk.Name);


                        dynamicCodeService.EmitPropertyService.WriteLambdaExpression(builder, ConstraintsMethodIL, columnsCLRType, new[] { fk.PropertyGetMethod });// fk.Select(c => c.PropertyGetMethod).ToArray());

                        var createTableMethod = options.CreateTableBuilderType.MakeGenericType(columnsCLRType)
                            .GetMethod(options.CreateTableBuilderForeignKeyName, BindingFlags.Public | BindingFlags.Instance, null,
                                new[] {
                                typeof(string),
                                typeof(Expression<>).MakeGenericType(
                                    typeof(Func<,>).MakeGenericType(columnsCLRType, typeof(object))),
                                typeof(string),typeof(string),typeof(string),
                                options.ReferentialActionType,options.ReferentialActionType }, null);

                        var principalSchema = fk.ReferenceType.Schema ?? options.Schema ?? "dbo";
                        var principalTable = fk.ReferenceType.CollectionSchemaName;
                        var principalColumn = fk.ReferenceType.Properties.SingleOrDefault(p => p.IsPrimaryKey)?.SchemaName;

                        if (string.IsNullOrEmpty(principalColumn))
                        {
                            throw new InvalidOperationException($"No reference type primary key defined for foreignkey {fk.ReferenceType.SchemaName} on {SchemaName}");
                        }

                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalTable);
                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalColumn);
                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalSchema);

                        ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, (int) fk.OnUpdateCascade); //OnUpdate
                        ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, (int) fk.OnDeleteCascade); //OnDelete


                        //
                        //onupdate
                        //ondelete
                        ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                        ConstraintsMethodIL.Emit(OpCodes.Pop);
                    }
                }



                ConstraintsMethodIL.Emit(OpCodes.Ret);








                var changingFromTPT2TPC = priorEntities.Any() &&
                    priorEntities.Last().GetCustomAttribute<EntityMappingStrategyAttribute>()?.MappingStrategy != MappingStrategy
                    && MappingStrategy == EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPC;

                bool changingFromTPT2TPCCalc()
                {
                    var c = this;
                    while (c != null)
                    {
                        var priorEntities = dynamicCodeService.GetTypes()
                       .Where(t => !t.IsPendingTypeBuilder() && t.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migrationAttribute && migrationAttribute.LogicalName == c.LogicalName && $"{migrationAttribute.Namespace}_{migrationAttribute.MigrationName}" != this.DynamicAssemblyBuilder.ModuleName)
                       .ToArray();


                        if (priorEntities.Any() &&
                            priorEntities.Last().GetCustomAttribute<EntityMappingStrategyAttribute>()?.MappingStrategy != c.MappingStrategy
                            && c.MappingStrategy == EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPC)
                            return true;

                        c = c.Parent;
                    }
                    return false;
                }


                if (changingFromTPT2TPC)
                {

                    //changingFromTPT2TPC = true;
                    //Need to drop Foreign Keys
                    /**
                     *   
                     *  /// <summary>
                        ///     Builds a <see cref="DropForeignKeyOperation" /> to drop an existing foreign key constraint.
                        /// </summary>
                        /// <remarks>
                        ///     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see> for more information and examples.
                        /// </remarks>
                        /// <param name="name">The name of the foreign key constraint to drop.</param>
                        /// <param name="table">The table that contains the foreign key.</param>
                        /// <param name="schema">The schema that contains the table, or <see langword="null" /> to use the default schema.</param>
                        /// <returns>A builder to allow annotations to be added to the operation.</returns>
                        public virtual OperationBuilder<DropForeignKeyOperation> DropForeignKey(
                            string name,
                            string table,
                            string? schema = null)
                     * 
                     * 
                     */
                    foreach (var fk in fKeys)
                    {
                        migrationBuilder.DropForeignKey(CollectionSchemaName, Schema, fk.Name);


                    }

                }


                if (!priorEntities.Any())
                {
                    dynamicCodeService.EmitPropertyService.CreateTableImpl(CollectionSchemaName, Schema, columnsCLRType, columsMethod, ConstraintsMethod, migrationBuilder.UpMethodIL);

                    //Create indexes from lookup fields.

                    dynamicCodeService.LookupPropertyBuilder.CreateLookupIndexes(migrationBuilder.UpMethodIL, this);

                }
                else if (members.Any())
                {

                    var hasPriorEntityColumnBuilders = dynamicCodeService.GetTypes().Where(t => !t.IsPendingTypeBuilder()
                       && t.GetCustomAttributes<EntityMigrationColumnsAttribute>()
                       .Any(migrationAttribute => migrationAttribute.LogicalName == LogicalName)).ToArray();

                    foreach (var newMember in members)
                    {
                        bool isBaseMember()
                        {
                            var c = this;
                            while (c.Parent != null)
                            {
                                if (c.Parent.Properties.Any(p => p.SchemaName == newMember.Value.Name))
                                    return true;
                                c = c.Parent;
                            }
                            return false;
                        }
                        var test = hasPriorEntityColumnBuilders.SelectMany(c => c.GetCustomAttributes<EntityMigrationColumnsAttribute>())
                             .Where(c => c.AttributeLogicalName == newMember.Key)
                             .ToArray();

                        var attributeDefinition = AllProperties//  entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                               .FirstOrDefault(attribute => attribute.LogicalName == newMember.Key);

                        //    var (typeObj, type) = GetTypeInfo(manifest, attributeDefinition);


                        if (!test.Any(c => c.MigrationName != migrationName))
                        {
                            //There are no other migration names than this one, its a new member.

                            if (attributeDefinition.PropertyType == null)
                                continue;

                            bool required = migrationBuilder.EmitAddColumn(CollectionSchemaName, Schema, attributeDefinition);


                            //TODO - when changing from TPT to TPC this should not be added for base foreignkeys
                            if (attributeDefinition.IsLookup)
                            {
                                dynamicCodeService.EmitPropertyService.AddForeignKey(CollectionSchemaName, Schema, migrationBuilder.UpMethodIL, attributeDefinition);

                                if (attributeDefinition.IndexInfo != null)
                                    dynamicCodeService.LookupPropertyBuilder.CreateLoopupIndex(migrationBuilder.UpMethodIL, CollectionSchemaName, Schema, attributeDefinition.SchemaName, attributeDefinition.IndexInfo); //.CreateLoopupIndex(options, EntityCollectionSchemaName, schema, UpMethodIL, attributeDefinition);
                            }

                            if (!attributeDefinition.IsRowVersion && isBaseMember() && changingFromTPT2TPCCalc())
                            {
                                var upSql1 = $@"UPDATE
                                [{Schema}].[{CollectionSchemaName}]
                                SET
                                    [{Schema}].[{CollectionSchemaName}].[{newMember.Value.Name}] = BaseRecords.[{newMember.Value.Name}]
                                FROM
                                    [{Schema}].[{CollectionSchemaName}] Records
                                INNER JOIN
                                    [{Schema}].[{Parent.CollectionSchemaName}] BaseRecords
                                ON 
                                    records.Id = BaseRecords.Id;";

                                EmitSQLUp(options, migrationBuilder.UpMethodIL, upSql1);

                                if (required)
                                {
                                    migrationBuilder.EmitAlterColumn(CollectionSchemaName, Schema, attributeDefinition);
                                }
                            }

                        }

                        else if (test.Length > 1)
                        {
                            var changes = test[test.Length - 2].GetChanges(test[test.Length - 1]);

                            if (changes.Any())
                            {
                                if (attributeDefinition.PropertyType == null)
                                    continue;

                                migrationBuilder.EmitAlterColumn(CollectionSchemaName, Schema, attributeDefinition);


                                //SOMETHING CHANGED



                            }

                            if (test[test.Length - 2].HasAttributeTypeChanged(test[test.Length - 1]) && attributeDefinition.IsLookup)
                            {


                                migrationBuilder.DropForeignKey(CollectionSchemaName, Schema, $"FK_{CollectionSchemaName}_{attributeDefinition.ReferenceType.CollectionSchemaName}_{attributeDefinition.SchemaName}".Replace(" ", ""));



                                dynamicCodeService.EmitPropertyService.AddForeignKey(CollectionSchemaName, Schema, migrationBuilder.UpMethodIL, attributeDefinition);

                            }

                        }
                    }
                }

                foreach (var fk in fKeys)
                {
                    /**
                     * We will skip the FKs if its a TPC and base class. See above comment
                     */
                    if (fk.ReferenceType.IsBaseEntity && fk.ReferenceType.MappingStrategy == EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPC)
                    {

                        //CREATE INDEX [IX_SecurityGroup_OwnerId] ON [SecurityGroup] ([OwnerId]);

                        migrationBuilder.CreateIndex(CollectionSchemaName, Schema, $"IX_{SchemaName}_{fk.AttributeSchemaName}", false, fk.AttributeSchemaName);


                        if (priorEntities.Any())
                        {
                            var priorBaseEntities = dynamicCodeService.GetTypes()
                               .Where(t => !t.IsPendingTypeBuilder() && t.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migrationAttribute
                                    && migrationAttribute.LogicalName == fk.ReferenceType.LogicalName)
                               .ToArray();

                            if (priorBaseEntities.LastOrDefault()?.GetCustomAttribute<EntityMappingStrategyAttribute>() is EntityMappingStrategyAttribute mappingStrategy
                                && mappingStrategy.OldMappingStrategy != mappingStrategy.MappingStrategy && mappingStrategy.MappingStrategy == EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPC)
                            {
                                migrationBuilder.DropForeignKey(CollectionSchemaName, Schema, fk.Name);
                            }
                        }



                    }
                }

                foreach (var key in Keys)
                {

                    if (priorEntities.Any(prior => prior?.GetCustomAttributes<EntityIndexAttribute>().Any(c => c.IndexName == key.Key) ?? false))
                    {
                        continue;
                    }


                    var props = key.Value;
                    var name = key.Key;
                    var colums = props.Select(p => Properties.Single(k => k.AttributeKey == p).SchemaName).ToArray();
                    migrationBuilder.CreateIndex(CollectionSchemaName, Schema, name, true, colums);

                }

                //  if (entityTypeBuilder.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migration && string.IsNullOrEmpty( migration.RawUpMigration))
                if (!string.IsNullOrEmpty(upSql))
                {
                    var alreadyExists = dynamicCodeService.GetTypes().FirstOrDefault(t =>
                          !t.IsPendingTypeBuilder() &&
                          t.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migrationAttribute &&
                          migrationAttribute.LogicalName == LogicalName && migrationAttribute.RawUpMigration == upSql);

                    if (alreadyExists == null)
                    {
                        EmitSQLUp(options, migrationBuilder.UpMethodIL, upSql);

                    }

                }

                migrationBuilder.UpMethodIL.Emit(OpCodes.Ret);

                var DownMethod = entityTypeBuilder.DefineMethod("Down", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderDropTable.DeclaringType });
                var DownMethodIL = DownMethod.GetILGenerator();

                if (!priorEntities.Any())
                {
                    DownMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                    DownMethodIL.Emit(OpCodes.Ldstr, CollectionSchemaName); //Constant
                    DownMethodIL.Emit(OpCodes.Ldstr, Schema);
                    DownMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderDropTable);
                    DownMethodIL.Emit(OpCodes.Pop);
                }
                DownMethodIL.Emit(OpCodes.Ret);



                return MigrationBuilder.CreateTypeInfo();
            }
        }

        private static void EmitSQLUp(CodeGenerationOptions options, ILGenerator UpMethodIL, string upSql1)
        {
            UpMethodIL.Emit(OpCodes.Ldarg_1);  //first argument
            UpMethodIL.Emit(OpCodes.Ldstr, upSql1);


            UpMethodIL.Emit(OpCodes.Ldc_I4_0);
            UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderSQL);
            UpMethodIL.Emit(OpCodes.Pop);
        }



        public void BuildDTOConfiguration()
        {
            var entityLogicalName = LogicalName;// entityDefinition.SelectToken("$.logicalName").ToString();
            var entitySchameName = SchemaName;
            var options = dynamicCodeService.Options;

            var entityTypeConfiguration = ConfigurationBuilder;


            entityTypeConfiguration.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { LogicalName }));

            var Configure2Method = entityTypeConfiguration.DefineMethod(options.EntityConfigurationConfigureName, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.EntityTypeBuilderType });
            var ConfigureMethod2IL = Configure2Method.GetILGenerator();



            if (IsBaseEntity && MappingStrategy.HasValue && MappingStrategy == EAVFW.Extensions.Manifest.SDK.DTO.MappingStrategy.TPC)
            {


                /**                     
                 * modelBuilder.Entity<Entity>().UseTpcMappingStrategy();
                 */
                ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
                ConfigureMethod2IL.Emit(OpCodes.Call, options.UseTpcMappingStrategy);
                ConfigureMethod2IL.Emit(OpCodes.Pop);


            }
            else
            {
                /**                     
                 * modelBuilder.Entity<Entity>().ToTable("PluralSchemaName");
                 */

                ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
                ConfigureMethod2IL.Emit(OpCodes.Ldstr, CollectionSchemaName); //Constant
                ConfigureMethod2IL.Emit(OpCodes.Ldstr, Schema ?? options.Schema ?? "dbo"); //Constant
                ConfigureMethod2IL.Emit(OpCodes.Call, options.EntityTypeBuilderToTable);
                ConfigureMethod2IL.Emit(OpCodes.Pop);
            }

            var isTablePerTypeChild = Parent != null;

            foreach (var propertyInfo in Properties)
            {

                var attributeSchemaName = propertyInfo.SchemaName;
                var isprimaryKey = propertyInfo.IsPrimaryKey;

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

                if (options.RequiredSupport && (propertyInfo.IsRequired))
                {
                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4_1);
                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.IsRequiredMethod);
                }

                if (propertyInfo.IsRowVersion)
                {
                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.IsRowVersionMethod);
                }


                if (propertyInfo.Type == "choice")
                {
                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, options.HasConversionMethod.MakeGenericMethod(typeof(int)));
                }



                if (propertyInfo.Type == "decimal" || propertyInfo.HasScaleInfo)
                {

                    //ConfigureMethod2IL.Emit(OpCodes.Ldc_I4, attributeDefinition.Value.SelectToken("$.type.type.sql.precision")?.ToObject<int>() ?? 18);
                    //ConfigureMethod2IL.Emit(OpCodes.Ldc_I4, attributeDefinition.Value.SelectToken("$.type.type.sql.scale")?.ToObject<int>() ?? 4);
                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4, propertyInfo.Precision);
                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4, propertyInfo.Scale);
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

            //   options.EntityDTOConfigurations[entityCollectionSchemaName] = entityTypeConfiguration.CreateTypeInfo();

        }
        public bool IsCreated { get; private set; }
        public TypeInfo CreateTypeInfo()
        {
            if (RemoteType != null)
            {
                return RemoteType;
            }



            if (IsCreated)
                return Builder.CreateTypeInfo();

            IsCreated = true;
#if DEBUG
            // File.AppendAllLines("test1.txt", new[] { $"Creating {SchemaName}" });
#endif
            try
            {
                Parent?.CreateTypeInfo();

                foreach (var dp in Dependencies)
                {
                    dp.CreateTypeInfo();
                }

                return Builder.CreateTypeInfo();
            }
            catch (Exception ex)
            {
#if DEBUG
                //  File.AppendAllLines("test1.txt", new[] { $"Failed {SchemaName}" });
                IsCreated = false;
#endif
                throw new InvalidOperationException($"Could not build {SchemaName}: {string.Join(",", Builder.GetInterfaces().Select(c => $"{c.Name}<{string.Join(",", c.GetGenericArguments().Select(t => $"{t.Name}<{t.BaseType.Name},{string.Join(",", t.GetInterfaces().Select(i => i.Name))}>"))}>"))}", ex);
            }
            finally
            {
#if DEBUG
                //   File.AppendAllLines("test1.txt", new[] { $"Created {SchemaName}" });
#endif
            }
        }

        public TypeInfo CreateConfigurationTypeInfo()
        {
            CreateTypeInfo();
            return ConfigurationBuilder.CreateTypeInfo();
        }
        public Type ClrParentType { get; private set; }
        private void BuildParent()
        {


            if (Parent?.Builder != null)
            {
                Parent.BuildType();
                this.Builder.SetParent(Parent?.Builder);

                return;
            }


            var staticParents = dynamicCodeService.FindParentClasses(this.EntityKey, properties.Values.Where(c => c.PropertyType != null).Select(c => c.SchemaName).ToArray());
            ClrParentType = staticParents;

            if (staticParents.IsGenericTypeDefinition)
            {

                var args = staticParents.GetCustomAttributes<GenericTypeArgumentAttribute>(false);

                //var types = args.Where(t => t.ManifestKey != SchemaName)
                //    .Select(t => DynamicAssemblyBuilder.Tables.FirstOrDefault(tt=>tt.Value.EntityKey == t.ManifestKey))
                //    .ToArray();


                //entityInfo.Dependencies.AddRange(types);
                // File.AppendAllLines("test1.txt", new[] { $"{string.Join(",", args.Select(t => t.ManifestKey))} => {string.Join(",", options.EntityDTOsBuilders.Keys)}" });

                // File.AppendAllLines("test1.txt", new[] { $"{acceptableBasesClass.FullName}<{string.Join(",", args.Select(t => t.ManifestKey == _ ? type.Name : options.EntityDTOsBuilders[manifest.SelectToken($"$.entities['{t.ManifestKey}'].schemaName").ToString()]?.Name).ToArray())}>" });



                Builder.SetParent(staticParents.MakeGenericType(args.Select(t => t.ManifestKey == SchemaName ? Builder : AddAsDependency(DynamicAssemblyBuilder.Tables.FirstOrDefault(tt => tt.Value.EntityKey == t.ManifestKey).Value).Builder).ToArray()));
                return;
            }


            Builder.SetParent(staticParents);

        }

        public HashSet<DynamicTableBuilder> Dependencies { get; } = new HashSet<DynamicTableBuilder>();
        public DynamicTableBuilder AddAsDependency(DynamicTableBuilder value)
        {
            Dependencies.Add(value);

            return value;
        }

        private ConcurrentBag<InverseLookupProp> InverseLookups { get; } = new ConcurrentBag<InverseLookupProp>();


        public void AddInverseLookup(string attributeKey, string propertySchemaName, DynamicTableBuilder dynamicTableBuilder)
        {
            InverseLookups.Add(new InverseLookupProp { AttributeKey = attributeKey, PropertySchemaName = propertySchemaName, Table = dynamicTableBuilder });

        }

        private void BuildInverseProperties()
        {
            foreach (var inverseg in InverseLookups.GroupBy(p => p.Table.CollectionSchemaName))
            {
                foreach (var inverse in inverseg)
                {
                    //  var propName = inverse.PropertySchemaName + inverse.Table.CollectionSchemaName;
                    var propName = inverseg.Count() > 1 || this.dynamicCodeService.Options.InversePropertyCollectionName == InversePropertyCollectionNamePattern.ConcatFieldNameAndLookupName ? inverse.PropertySchemaName + inverse.Table.CollectionSchemaName : inverse.Table.CollectionSchemaName;
                    var propBuilder = AddProperty(null, propName, propName.ToLower(), typeof(ICollection<>).MakeGenericType(inverse.Table.Builder));  // CreateProperty(entityType, (attributes.Length > 1 ? attribute.Name.Replace(" ", "") : "") + entity.Value.SelectToken("$.collectionSchemaName")?.ToString(), typeof(ICollection<>).MakeGenericType(related.Builder));
                                                                                                                                                      // methodAttributes: MethodAttributes.Virtual| MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig);

                    propBuilder.AddInverseAttribute(inverse.PropertySchemaName);

                }


            }
        }


        public Dictionary<string, string[]> Keys { get; } = new Dictionary<string, string[]>();
        public MappingStrategy? MappingStrategy { get; private set; }

        public DynamicTableBuilder SetMappingStrategry(MappingStrategy mappingStrategy)
        {
            this.MappingStrategy = mappingStrategy;
            return this;
        }

        internal void AddKeys(string name, string[] props)
        {
            this.Keys.Add(name, props);
        }

        internal DynamicTableBuilder External(bool v, TypeInfo remoteType)
        {
            IsExternal = v;
            RemoteType = remoteType;
            return this;
        }


        internal DynamicTableBuilder WithSQLUp(string upSql)
        {
            SQLUpStatements.Add(upSql);
            return this;
        }

        internal void FinalizeType()
        {

            foreach (var prop in properties.Values.OrderByDescending(c => c.IsPrimaryKey).ThenByDescending(c => c.IsPrimaryField).ThenBy(c => c.LogicalName))
            {
                prop.AddInterfaceOverrides();
            }
        }

        internal void DeppendsOn(Type dependency)
        {
             if(this.DynamicAssemblyBuilder.Tables.Any(other => other.Value.Builder == dependency))
            {
                this.AddAsDependency(this.DynamicAssemblyBuilder.Tables.Values.First(other => other.Builder == dependency));
            }
        }
    }

}
