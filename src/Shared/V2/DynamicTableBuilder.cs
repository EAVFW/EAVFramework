

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace EAVFramework.Shared.V2
{
    public class DynamicTableBuilder : IDynamicTableBuilder
    {
        protected ConcurrentDictionary<string, DynamicPropertyBuilder> properties = new ConcurrentDictionary<string, DynamicPropertyBuilder>();

        public IReadOnlyCollection<DynamicPropertyBuilder> Properties => properties.Values.ToArray();

        private DynamicCodeService dynamicCodeService;
        private ModuleBuilder myModule;
        public DynamicAssemblyBuilder DynamicAssemblyBuilder { get; }
        
        public string SchemaName { get; }
        public TypeBuilder Builder { get; }
        public TypeBuilder ConfigurationBuilder { get; }

      //  public TypeBuilder MigrationBuilder { get; }

        public string CollectionSchemaName { get; }
        public string LogicalName { get; }
        public bool IsBaseEntity { get; }
        public string Schema { get; }

        public string EntityKey { get; }

        public bool IsExternal { get; }

        public DynamicTableBuilder(
            DynamicCodeService dynamicCodeService,
            ModuleBuilder myModule,
            DynamicAssemblyBuilder dynamicAssemblyBuilder,
            string entityKey,
            string tableSchemaName, string tableLogicalName, string collectionSchemaName, string schema = "dbo", bool isAbstract = false)
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

            
            Builder = myModule.DefineType($"{dynamicAssemblyBuilder.Namespace}.{SchemaName}", TypeAttributes.Public
                                                                        | (isAbstract ? TypeAttributes.Class : TypeAttributes.Class)
                                                                        | TypeAttributes.AutoClass
                                                                        | TypeAttributes.AnsiClass
                                                                        | TypeAttributes.Serializable
                                                                        | TypeAttributes.BeforeFieldInit);
            ConfigurationBuilder= myModule.DefineType($"{DynamicAssemblyBuilder.Namespace}.{SchemaName}Configuration", TypeAttributes.Public
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
                        typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.IsBaseClass))

                    }, new object[] {
                   LogicalName ,
                   SchemaName,
                   CollectionSchemaName,
                   IsBaseEntity
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
            if (ContainsProperty(propertyName))
                return null;

            return properties.GetOrAdd(propertyName, (_) => new DynamicPropertyBuilder(dynamicCodeService, this, attributeKey, propertyName, logicalName, type));
        }
        public DynamicPropertyBuilder AddProperty(string attributeKey, string propertyName, string logicalName, Type type)
        {
            if (ContainsProperty(propertyName))
                return null;

            return properties.GetOrAdd(propertyName, (_) => new DynamicPropertyBuilder(dynamicCodeService, this, attributeKey, propertyName, logicalName, type));
        }
        public bool ContainsProperty(string propertyName)
        {
            if (properties.ContainsKey(propertyName) || (Parent?.ContainsProperty(propertyName)??false))
            {
                return true;
            }
            return false;
        }

        public DynamicTableBuilder WithTable(string entityKey, string tableSchemaName, string tableLogicalName, string tableCollectionSchemaName, string schema, bool isBaseClass)
        {
            return this.DynamicAssemblyBuilder.WithTable(entityKey,tableSchemaName, tableLogicalName, tableCollectionSchemaName, schema, isBaseClass);
        }
        public DynamicTableBuilder Parent { get; private set; }

        public string[] AllPropsWithLookups => this.properties.Keys.Concat(this.Parent?.AllPropsWithLookups ?? Enumerable.Empty<string>()).ToArray();
        public TypeInfo GetTypeInfo()
        {
            return Builder.GetTypeInfo();
        }

        
        public void BuildType()
        {
            BuildParent();
            BuildInverseProperties();

            foreach (var prop in Properties)
            {
                prop.Build();
            }
              
            AddInterfaces();

            BuildDTOConfiguration();

           // BuildMigration();
        }


        public Type CreateMigrationType (string migrationName)
        {
            var MigrationBuilder = myModule.DefineType($"{DynamicAssemblyBuilder.Namespace}.{CollectionSchemaName}Builder_{migrationName.Replace(".", "_")}", TypeAttributes.Public);
            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { LogicalName });
            MigrationBuilder.SetCustomAttribute(EntityAttributeBuilder);

            var builder = DynamicAssemblyBuilder.Module;
            var options = dynamicCodeService.Options;
            var entityTypeBuilder = MigrationBuilder;

            var upSql = string.Join("\n", SQLUpStatements);
          //  if (options.PartOfMigration)
            {


                CustomAttributeBuilder EntityMigrationAttributeBuilder = new CustomAttributeBuilder(
                    typeof(EntityMigrationAttribute).GetConstructor(new Type[] { }),
                    new object[] { },
                    new[] {
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.LogicalName)),
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.MigrationName)) ,
                        typeof(EntityMigrationAttribute).GetProperty(nameof(EntityMigrationAttribute.RawUpMigration))
                    },


                    new[] { LogicalName, migrationName, upSql });
                entityTypeBuilder.SetCustomAttribute(EntityMigrationAttributeBuilder);
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

                var (columnsCLRType, columnsctor, members) = CreateColumnsType(migrationName);

                var columsMethod = entityTypeBuilder.DefineMethod("Columns", MethodAttributes.Public, columnsCLRType, new[] { options.ColumnsBuilderType });

                var columsMethodIL = columsMethod.GetILGenerator();
                columsMethodIL.Emit(OpCodes.Ldarg_1);
                columsMethodIL.Emit(OpCodes.Newobj, columnsctor);
                columsMethodIL.Emit(OpCodes.Ret);


                var ConstraintsMethod = entityTypeBuilder.DefineMethod("Constraints", MethodAttributes.Public, null, new[] { options.CreateTableBuilderType.MakeGenericType(columnsCLRType) });
                var ConstraintsMethodIL = ConstraintsMethod.GetILGenerator();

                //var primaryKeys = entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                //.Where(attribute => attribute.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)
                // .Where(attribute => members.ContainsKey(attribute.Value.SelectToken("$.logicalName")?.ToString()))
                //.Select(attribute => members[attribute.Value.SelectToken("$.logicalName")?.ToString()].GetMethod)
                //.ToArray();
                var primaryKeys = Properties.Where(p => p.IsPrimaryKey)
                    .Where(p=>members.ContainsKey(p.LogicalName))
                    .Select(p => members[p.LogicalName].GetMethod)
                    .ToArray();



                var fKeys = Properties.Where(p=>p.IsLookup) // entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                                                            //  .Where(attribute => attribute.Value.SelectToken("$.type.type")?.ToString() == "lookup")
                                                            // .Where(attribute => members.ContainsKey(attribute.Value.SelectToken("$.logicalName")?.ToString()))
                      .Where(attribute => members.ContainsKey(attribute.LogicalName))
                   .Select(attribute => new
                   {
                       AttributeSchemaName = attribute.SchemaName,  //attribute.Value.SelectToken("$.schemaName").ToString(),
                       PropertyGetMethod = members[attribute.LogicalName].GetMethod,
                       ReferenceType = attribute.ReferenceType,
                       OnDeleteCascade = attribute.OnDeleteCascade ?? options.ReferentialActionNoAction,
                       OnUpdateCascade = attribute.OnUpdateCascade ?? options.ReferentialActionNoAction,
                      // ForeignKey = attribute.ForeignKey
                   })
                   .ToArray();

                if (primaryKeys.Any() || fKeys.Any())
                {
                    ConstraintsMethodIL.DeclareLocal(typeof(ParameterExpression));
                }

                if (primaryKeys.Any())
                {

                    ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, $"PK_{CollectionSchemaName}"); //PK Name

                    dynamicCodeService.EmitPropertyService.WriteLambdaExpression(builder, ConstraintsMethodIL, columnsCLRType, primaryKeys);

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
                      //  var entityName = fk.EntityName;
                        ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                        ConstraintsMethodIL.Emit(OpCodes.Ldstr, $"FK_{CollectionSchemaName}_{fk.ReferenceType.CollectionSchemaName}_{fk.AttributeSchemaName}".Replace(" ", ""));

                        // Console.WriteLine($"FK_{EntityCollectionSchemaName}_{manifest.SelectToken($"$.entities['{entityName}'].pluralName")}_{fk.AttributeSchemaName}".Replace(" ", ""));


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
                        var principalColumn = fk.ReferenceType.Properties.Single(p => p.IsPrimaryKey).SchemaName;  

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



                ConstraintsMethodIL.Emit(OpCodes.Ret);

                
                var UpMethod = entityTypeBuilder.DefineMethod("Up", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderCreateTable.DeclaringType });

                var UpMethodIL = UpMethod.GetILGenerator();

                var hasPriorEntity = builder.GetTypes().FirstOrDefault(t => !t.IsPendingTypeBuilder() && t.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migrationAttribute && migrationAttribute.LogicalName == LogicalName);


                if (hasPriorEntity == null)
                {
                   dynamicCodeService.EmitPropertyService.CreateTableImpl(CollectionSchemaName, Schema, columnsCLRType, columsMethod, ConstraintsMethod, UpMethodIL);

                    //Create Indexes
                    //alternativ keys //TODO create dropindex
                   
                   
                    {
                        foreach (var key in Keys)
                        {
                            var props = key.Value;

                            try
                            {
                                UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                                UpMethodIL.Emit(OpCodes.Ldstr, key.Key); //Constant keyname 
                                UpMethodIL.Emit(OpCodes.Ldstr, CollectionSchemaName); //Constant table name


                                UpMethodIL.Emit(OpCodes.Ldc_I4, props.Length); // Array length
                                UpMethodIL.Emit(OpCodes.Newarr, typeof(string));
                                for (var j = 0; j < props.Length; j++)
                                {
                                    var attributeDefinition = Properties.Single(k => k.AttributeKey == props[j]);// entityDefinition.Value.SelectToken($"$.attributes['{props[j]}']");
                                    var attributeSchemaName = attributeDefinition.SchemaName;
                                    UpMethodIL.Emit(OpCodes.Dup);
                                    UpMethodIL.Emit(OpCodes.Ldc_I4, j);
                                    UpMethodIL.Emit(OpCodes.Ldstr, attributeSchemaName);
                                    UpMethodIL.Emit(OpCodes.Stelem_Ref);
                                }



                                UpMethodIL.Emit(OpCodes.Ldstr, Schema); //Constant schema
                                UpMethodIL.Emit(OpCodes.Ldc_I4_1); //Constant unique=true
                                UpMethodIL.Emit(OpCodes.Ldnull); //Constant filter=null


                                UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderCreateIndex);
                                UpMethodIL.Emit(OpCodes.Pop);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to create key for {CollectionSchemaName}.{key.Key}", ex);
                            }
                        }
                    }

                    //Create indexes from lookup fields.

                    dynamicCodeService.LookupPropertyBuilder.CreateLookupIndexes( UpMethodIL,this);

                }
                else if (members.Any())
                {
                    var hasPriorEntityColumnBuilders = builder.GetTypes().Where(t => !t.IsPendingTypeBuilder()
                       && t.GetCustomAttributes<EntityMigrationColumnsAttribute>()
                       .Any(migrationAttribute => migrationAttribute.LogicalName == LogicalName)).ToArray();

                    foreach (var newMember in members)
                    {

                        var test = hasPriorEntityColumnBuilders.SelectMany(c => c.GetCustomAttributes<EntityMigrationColumnsAttribute>())
                             .Where(c => c.AttributeLogicalName == newMember.Key)
                             .ToArray();

                        var attributeDefinition = Properties//  entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                               .FirstOrDefault(attribute => attribute.LogicalName == newMember.Key);

                    //    var (typeObj, type) = GetTypeInfo(manifest, attributeDefinition);


                        if (!test.Any(c => c.MigrationName != migrationName))
                        {
                            //There are no other migration names than this one, its a new member.

                            if (attributeDefinition.PropertyType == null)
                                continue;


                            var method = options.MigrationsBuilderAddColumn.MakeGenericMethod(attributeDefinition.PropertyType); 
                           

                            UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                                                              //MigrationsBuilderAddColumn

                            BuildParametersForcolumn(UpMethodIL, attributeDefinition, method, CollectionSchemaName, Schema);

                            UpMethodIL.Emit(OpCodes.Callvirt, method);
                            UpMethodIL.Emit(OpCodes.Pop);


                            if (attributeDefinition.IsLookup)
                            {
                               dynamicCodeService.EmitPropertyService.AddForeignKey(CollectionSchemaName, Schema, UpMethodIL, attributeDefinition);

                                if(attributeDefinition.IndexInfo!=null)
                                    dynamicCodeService.LookupPropertyBuilder.CreateLoopupIndex(UpMethodIL,CollectionSchemaName,Schema,attributeDefinition.SchemaName,attributeDefinition.IndexInfo); //.CreateLoopupIndex(options, EntityCollectionSchemaName, schema, UpMethodIL, attributeDefinition);
                            }
                        }

                        else if (test.Length > 1)
                        {
                            var changes = test[test.Length - 2].GetChanges(test[test.Length - 1]);

                            if (changes.Any())
                            {
                                if (attributeDefinition.PropertyType == null)
                                    continue;

                                var method = options.MigrationsBuilderAlterColumn.MakeGenericMethod(attributeDefinition.PropertyType);
                               

                                UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                                                                  //MigrationsBuilderAddColumn

                                BuildParametersForcolumn(UpMethodIL, attributeDefinition, method, CollectionSchemaName, Schema);

                                UpMethodIL.Emit(OpCodes.Callvirt, method);
                                UpMethodIL.Emit(OpCodes.Pop);


                                //SOMETHING CHANGED



                            }

                            if (test[test.Length - 2].HasAttributeTypeChanged(test[test.Length - 1]) && attributeDefinition.IsLookup)
                            {

                                UpMethodIL.Emit(OpCodes.Ldarg_1);
                                var tableName = CollectionSchemaName;
                                foreach (var arg1 in options.MigrationsBuilderDropForeignKey.GetParameters())
                                {
                                    var argName = arg1.Name;


                                    switch (argName)
                                    {
                                        case "table" when !string.IsNullOrEmpty(tableName): UpMethodIL.Emit(OpCodes.Ldstr, tableName); break;
                                        case "schema" when !string.IsNullOrEmpty(Schema): dynamicCodeService.EmitPropertyService.EmitNullable(UpMethodIL, () => UpMethodIL.Emit(OpCodes.Ldstr, Schema), arg1); break;
                                        case "name": UpMethodIL.Emit(OpCodes.Ldstr, $"FK_{CollectionSchemaName}_{attributeDefinition.ReferenceType.CollectionSchemaName}_{attributeDefinition.SchemaName}".Replace(" ", "")); break;
                                    }
                                }


                                UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationsBuilderDropForeignKey);
                                UpMethodIL.Emit(OpCodes.Pop);

                                dynamicCodeService.EmitPropertyService.AddForeignKey( CollectionSchemaName, Schema, UpMethodIL, attributeDefinition);

                            }

                        }
                    }
                }

                //  if (entityTypeBuilder.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migration && string.IsNullOrEmpty( migration.RawUpMigration))
                if (!string.IsNullOrEmpty(upSql))
                {
                    var alreadyExists = builder.GetTypes().FirstOrDefault(t =>
                          !t.IsPendingTypeBuilder() &&
                          t.GetCustomAttribute<EntityMigrationAttribute>() is EntityMigrationAttribute migrationAttribute &&
                          migrationAttribute.LogicalName == LogicalName && migrationAttribute.RawUpMigration == upSql);

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
                    DownMethodIL.Emit(OpCodes.Ldstr, CollectionSchemaName); //Constant
                    DownMethodIL.Emit(OpCodes.Ldstr, Schema);
                    DownMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderDropTable);
                    DownMethodIL.Emit(OpCodes.Pop);
                }
                DownMethodIL.Emit(OpCodes.Ret);

            }

            return MigrationBuilder.CreateTypeInfo();
        }

        private (Type, ConstructorBuilder, Dictionary<string, PropertyBuilder>) CreateColumnsType( string migrationName)
        {
            var builder = DynamicAssemblyBuilder.Module;
            var options = dynamicCodeService.Options;

            var logicalName = LogicalName;// entityDefinition.SelectToken("$.logicalName").ToString();

            var members = new Dictionary<string, PropertyBuilder>();
            
            var columnsType = builder.DefineType($"{DynamicAssemblyBuilder.Namespace}.{SchemaName}Columns_{migrationName.Replace(".", "_")}", TypeAttributes.Public);




            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { LogicalName });
            columnsType.SetCustomAttribute(EntityAttributeBuilder);



            var dfc = columnsType.DefineDefaultConstructor(MethodAttributes.Public);

            ConstructorBuilder entityCtorBuilder =
                  columnsType.DefineConstructor(MethodAttributes.Public,
                                     CallingConventions.Standard, new[] { options.ColumnsBuilderType });



            ILGenerator entityCtorBuilderIL = entityCtorBuilder.GetILGenerator();

            entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityCtorBuilderIL.Emit(OpCodes.Call, dfc);



           // foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
            foreach(var propertyInfo in Properties)
            {
                var attributeLogicalName = propertyInfo.LogicalName;// attributeDefinition.Value.SelectToken("$.logicalName")?.ToString();
                var attributeSchemaName = propertyInfo.SchemaName;// attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");

                if (propertyInfo.PropertyType == null)
                    continue;

                var method = options.ColumnsBuilderColumnMethod.MakeGenericMethod(propertyInfo.PropertyType);
                 

                entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
                entityCtorBuilderIL.Emit(OpCodes.Ldarg_1);

                var (attProp, attField) = dynamicCodeService.EmitPropertyService.CreateProperty(columnsType, attributeSchemaName, options.OperationBuilderAddColumnOptionType); //CreateProperty(entityType, attributeSchemaName, options.OperationBuilderAddColumnOptionType);



                var columparams = BuildParametersForcolumn(entityCtorBuilderIL, propertyInfo, method);

                entityCtorBuilderIL.Emit(OpCodes.Callvirt, method);
                entityCtorBuilderIL.Emit(OpCodes.Call, attProp.SetMethod);


                if (options.PartOfMigration)
                {
                    var attributeProperties = columparams.Keys.Concat(new[] {
                            typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.LogicalName)),
                            typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.MigrationName)),
                            typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.AttributeLogicalName)),
                            typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.AttributeHash)),
                             typeof(EntityMigrationColumnsAttribute).GetProperty(nameof(EntityMigrationColumnsAttribute.AttributeTypeHash))
                        }).ToArray();
                    var attributesValues = columparams.Values.Concat(new[] {
                               LogicalName,
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
                if(value != null)
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
                        case "type" when propertyInfo.Type == "multilinetext":
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, "nvarchar(max)"), arg1);
                            break;

                        case "type" when propertyInfo.Type == "text" && !propertyInfo.MaxLength.HasValue:
                        case "type" when propertyInfo.Type == "string" && !propertyInfo.MaxLength.HasValue:
                            dynamicCodeService.EmitPropertyService.EmitNullable(entityCtorBuilderIL, () => entityCtorBuilderIL.Emit(OpCodes.Ldstr, $"nvarchar({((propertyInfo.IsPrimaryKey) ? 255 : 100)})"), arg1);
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


        public void BuildDTOConfiguration()
        {
            var entityLogicalName = LogicalName;// entityDefinition.SelectToken("$.logicalName").ToString();
            var entitySchameName = SchemaName;
            var options = dynamicCodeService.Options;

            var entityTypeConfiguration = ConfigurationBuilder;


            entityTypeConfiguration.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { LogicalName }));

            var Configure2Method = entityTypeConfiguration.DefineMethod(options.EntityConfigurationConfigureName, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.EntityTypeBuilderType });
            var ConfigureMethod2IL = Configure2Method.GetILGenerator();




            ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
            ConfigureMethod2IL.Emit(OpCodes.Ldstr, CollectionSchemaName); //Constant
            ConfigureMethod2IL.Emit(OpCodes.Ldstr, Schema ?? options.Schema ?? "dbo"); //Constant
            ConfigureMethod2IL.Emit(OpCodes.Call, options.EntityTypeBuilderToTable);
            ConfigureMethod2IL.Emit(OpCodes.Pop);


            var isTablePerTypeChild = Parent != null;// !string.IsNullOrEmpty(entityDefinition.SelectToken($"$.TPT")?.ToString());


            // foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
            foreach (var propertyInfo in Properties)
            {
                //if (attributeDefinition.Value.SelectToken("$.type.type")?.ToString().ToLower() == "choices")
                //    continue;

                var attributeSchemaName = propertyInfo.SchemaName;//  attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");
                var isprimaryKey = propertyInfo.IsPrimaryKey; // attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;

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
                    ConfigureMethod2IL.Emit(OpCodes.Ldc_I4,propertyInfo.Scale);
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

        public TypeInfo CreateTypeInfo()
        {   
            Parent?.CreateTypeInfo();
            return Builder.CreateTypeInfo();
        }

        public TypeInfo CreateConfigurationTypeInfo()
        {
            CreateTypeInfo();
            return ConfigurationBuilder.CreateTypeInfo();
        }

        private void BuildParent()
        {


            if (Parent?.Builder != null)
            {
                this.Builder.SetParent(Parent?.Builder);
                
                return;
            }

             
            var staticParents = dynamicCodeService.FindParentClasses(this.EntityKey, Properties.Select(c=>c.SchemaName).ToArray());


            if (staticParents.IsGenericTypeDefinition)
            {

                var args = staticParents.GetCustomAttributes<GenericTypeArgumentAttribute>(false);

                //var types = args.Where(t => t.ManifestKey != SchemaName)
                //    .Select(t => DynamicAssemblyBuilder.Tables.FirstOrDefault(tt=>tt.Value.EntityKey == t.ManifestKey))
                //    .ToArray();


                //entityInfo.Dependencies.AddRange(types);
                // File.AppendAllLines("test1.txt", new[] { $"{string.Join(",", args.Select(t => t.ManifestKey))} => {string.Join(",", options.EntityDTOsBuilders.Keys)}" });

                // File.AppendAllLines("test1.txt", new[] { $"{acceptableBasesClass.FullName}<{string.Join(",", args.Select(t => t.ManifestKey == _ ? type.Name : options.EntityDTOsBuilders[manifest.SelectToken($"$.entities['{t.ManifestKey}'].schemaName").ToString()]?.Name).ToArray())}>" });

                
               
                Builder.SetParent(staticParents.MakeGenericType(args.Select(t => t.ManifestKey == SchemaName ? Builder : DynamicAssemblyBuilder.Tables.FirstOrDefault(tt => tt.Value.EntityKey == t.ManifestKey).Value.Builder).ToArray()));
                return;
            }


            Builder.SetParent(staticParents);

        }

       
        private ConcurrentBag<InverseLookupProp> InverseLookups { get; } = new ConcurrentBag<InverseLookupProp>();
      

        public void AddInverseLookup(string attributeKey, string propertySchemaName, DynamicTableBuilder dynamicTableBuilder)
        {
            InverseLookups.Add(new InverseLookupProp { AttributeKey = attributeKey,  PropertySchemaName = propertySchemaName, Table = dynamicTableBuilder });

        }

        private void BuildInverseProperties()
        {
            foreach (var inverse in InverseLookups)
            {
                var propName = inverse.PropertySchemaName + inverse.Table.CollectionSchemaName;
                var propBuilder = AddProperty(null, propName, propName.ToLower(), typeof(ICollection<>).MakeGenericType(inverse.Table.Builder));  // CreateProperty(entityType, (attributes.Length > 1 ? attribute.Name.Replace(" ", "") : "") + entity.Value.SelectToken("$.collectionSchemaName")?.ToString(), typeof(ICollection<>).MakeGenericType(related.Builder));
                // methodAttributes: MethodAttributes.Virtual| MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig);

                propBuilder.AddInverseAttribute(inverse.PropertySchemaName);




            }
        }


        public Dictionary<string, string[]> Keys { get; } = new Dictionary<string, string[]>();
        internal void AddKeys(string name, string[] props)
        {
            this.Keys.Add(name, props);
        }
    }
}
