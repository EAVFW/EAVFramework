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

    public class EntityAttribute : Attribute
    {
        public string LogicalName { get; set; }
        public string SchemaName { get; set; }
        public string CollectionSchemaName { get; set; }
    }
    public class EntityDTOAttribute : Attribute
    {
        public string LogicalName { get; set; }
    }
    public class BaseEntityAttribute : Attribute
    {
       
    }

    public class CodeGeneratorOptions
    {
        public ModuleBuilder myModule { get; set; }
        public string Namespace { get; set; }
        public string migrationName { get; set; }

        public MethodInfo MigrationBuilderCreateTable { get; set; }
        public Type ColumnsBuilderType { get;  set; }
        public Type CreateTableBuilderType { get;  set; }
        public Type EntityTypeBuilderType { get;  set; }
        public MethodInfo EntityTypeBuilderPropertyMethod { get;  set; }
        public MethodInfo EntityTypeBuilderToTable { get;  set; }
        public MethodInfo EntityTypeBuilderHasKey { get;  set; }
        public string Schema { get; set; }
        public ConstructorInfo ForeignKeyAttributeCtor { get; internal set; }
        public Dictionary<string, Type> EntityDTOs { get; internal set; } = new Dictionary<string, Type>();
        public ConcurrentDictionary<string, TypeBuilder> EntityDTOsBuilders { get; internal set; } = new ConcurrentDictionary<string, TypeBuilder>();
        public Dictionary<string,Type> EntityDTOConfigurations { get; internal set; } = new Dictionary<string, Type>();
        public Type OperationBuilderAddColumnOptionType { get; internal set; }
        public MethodInfo ColumnsBuilderColumnMethod { get; internal set; }
        public MethodInfo LambdaBase { get; internal set; }
       
        public Type EntityConfigurationInterface { get; internal set; }
        public string EntityConfigurationConfigureName { get; internal set; }
        public ConstructorInfo JsonPropertyNameAttributeCtor { get; internal set; }
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
        public Assembly DTOAssembly { get;  set; }
        public Type[] DTOBaseClasses { get; internal set; }

        public Action<JToken,PropertyBuilder> OnDTOTypeGeneration { get; set; }
    }

    public interface ICodeGenerator
    {

    }
    public class CodeGenerator : ICodeGenerator
    {

        private readonly CodeGeneratorOptions options;

        public  CodeGenerator(CodeGeneratorOptions options)
        {
            this.options = options;
        }

        public Dictionary<string, StringBuilder> methodBodies = new Dictionary<string, StringBuilder>();


        public Type BuildEntityDefinition(ModuleBuilder builder, JToken manifest, JProperty entityDefinition)
        {
            var EntitySchameName = entityDefinition.Name.Replace(" ", "");
            var EntityCollectionSchemaName = (entityDefinition.Value.SelectToken("$.pluralName")?.ToString() ?? EntitySchameName).Replace(" ", "");
            //   AppDomain myDomain = AppDomain.CurrentDomain;
            //   AssemblyName myAsmName = new AssemblyName("MigrationTable" + entity.Name + "Assembly");

            //  var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
            //    AssemblyBuilderAccess.Run);



            //  ModuleBuilder myModule =
            //    builder.DefineDynamicModule(myAsmName.Name);

            CreateDTO(builder, EntityCollectionSchemaName, EntitySchameName, entityDefinition.Value as JObject, manifest);
            CreateDTOConfiguration(builder, EntityCollectionSchemaName, EntitySchameName, entityDefinition.Value as JObject, manifest);

            var (columnsCLRType, columnsctor, members) = CreateColumnsType(manifest, EntitySchameName, EntityCollectionSchemaName, entityDefinition.Value as JObject, builder);

            //   var test = Activator.CreateInstance(entityClrType,new[] { new ColumnsBuilder(new CreateTableOperation()) });

            TypeBuilder entityTypeBuilder =
             builder.DefineType($"{options.Namespace}.{EntityCollectionSchemaName}Builder", TypeAttributes.Public);

            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { entityDefinition.Value.SelectToken("$.logicalName").ToString() });
            entityTypeBuilder.SetCustomAttribute(EntityAttributeBuilder);

            entityTypeBuilder.AddInterfaceImplementation(options.DynamicTableType);

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




            var columsMethod = entityTypeBuilder.DefineMethod("Columns", MethodAttributes.Public, columnsCLRType, new[] {options.ColumnsBuilderType});

            var columsMethodIL = columsMethod.GetILGenerator();
            columsMethodIL.Emit(OpCodes.Ldarg_1);
            columsMethodIL.Emit(OpCodes.Newobj, columnsctor);
            columsMethodIL.Emit(OpCodes.Ret);


            var ConstraintsMethod = entityTypeBuilder.DefineMethod("Constraints", MethodAttributes.Public, null, new[] {options.CreateTableBuilderType.MakeGenericType(columnsCLRType) });
            var ConstraintsMethodIL = ConstraintsMethod.GetILGenerator();

            var primaryKeys = entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                .Where(attribute => attribute.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)
                .Select(attribute => members[attribute.Name].GetMethod)
                .ToArray();


            var fKeys = entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
               .Where(attribute => attribute.Value.SelectToken("$.type.type")?.ToString() == "lookup")
               .Select(attribute => new { AttributeSchemaName= attribute.Value.SelectToken("$.schemaName").ToString(), PropertyGetMethod = members[attribute.Name].GetMethod, EntityName = attribute.Value.SelectToken("$.type.referenceType").ToString(), ForeignKey = attribute.Value.SelectToken("$.type.foreignKey") })
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

                    var entityName = fk.EntityName;
                    ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, $"FK_{EntityCollectionSchemaName}_{manifest.SelectToken($"$.entities['{entityName}'].pluralName")}_{fk.AttributeSchemaName}".Replace(" ", ""));

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

                    ConstraintsMethodIL.Emit(OpCodes.Ldc_I4,options.ReferentialActionNoAction);
                    ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, options.ReferentialActionNoAction);


                    //
                    //onupdate
                    //ondelete
                    ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                    ConstraintsMethodIL.Emit(OpCodes.Pop);
                }
            }



            ConstraintsMethodIL.Emit(OpCodes.Ret);

            var schema = entityDefinition.Value.SelectToken("$.schama")?.ToString() ?? options.Schema ?? "dbo";

            var UpMethod = entityTypeBuilder.DefineMethod("Up", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderCreateTable.DeclaringType });

            var UpMethodIL = UpMethod.GetILGenerator();

            CreateTableImpl(EntityCollectionSchemaName, schema, columnsCLRType, columsMethod, ConstraintsMethod, UpMethodIL);

            UpMethodIL.Emit(OpCodes.Ret);


          
            var DownMethod = entityTypeBuilder.DefineMethod("Down", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { options.MigrationBuilderDropTable.DeclaringType });
            var DownMethodIL = DownMethod.GetILGenerator();
            DownMethodIL.Emit(OpCodes.Ldarg_1); //first argument
            DownMethodIL.Emit(OpCodes.Ldstr, EntityCollectionSchemaName); //Constant
            DownMethodIL.Emit(OpCodes.Ldstr, schema);
            DownMethodIL.Emit(OpCodes.Callvirt, options.MigrationBuilderDropTable);
            DownMethodIL.Emit(OpCodes.Pop);
            DownMethodIL.Emit(OpCodes.Ret);

            var type = entityTypeBuilder.CreateTypeInfo();

            return type;
        }

        internal Type CreateDynamicMigration(JToken manifest)
        {
            TypeBuilder migrationType =
                                         options.myModule.DefineType($"{options.Namespace}.Migration{options.migrationName}", TypeAttributes.Public,options.DynamicMigrationType);


             
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

        public virtual Type GetCLRType(JToken attributeDefinition, out string manifestType)
        {
            var typeObj = attributeDefinition.SelectToken("$.type");
            manifestType = typeObj.ToString();
            if (typeObj.Type == JTokenType.Object)
            {
                manifestType = typeObj.SelectToken("$.type").ToString();
            }

            manifestType = manifestType.ToLower();
            return GetCLRType(manifestType);
        }
        public virtual Type GetCLRType(string manifestType)
        {
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
                    return typeof(DateTime?);
                case "boolean":
                    return typeof(bool?);
                case "choice":
                    return typeof(int?);
                case "customer":
                    return typeof(Guid?);
            }
            return null;
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
            ConfigureMethod2IL.Emit(OpCodes.Ldstr, entityDefinition.SelectToken("$.schama")?.ToString() ?? options.Schema ?? "dbo"); //Constant
            ConfigureMethod2IL.Emit(OpCodes.Call, options.EntityTypeBuilderToTable);
            ConfigureMethod2IL.Emit(OpCodes.Pop);

            foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
            {
                var attributeSchemaName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");
                var isprimaryKey = attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;
                if (isprimaryKey)
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

                ConfigureMethod2IL.Emit(OpCodes.Pop);



            }

            ConfigureMethod2IL.Emit(OpCodes.Ret);

            options.EntityDTOConfigurations[entityCollectionSchemaName] = entityTypeConfiguration.CreateTypeInfo();

        }

            public void CreateDTO(ModuleBuilder myModule, string entityCollectionSchemaName, string entitySchameName, JObject entityDefinition, JToken manifest)
        {
            // var members = new Dictionary<string, PropertyBuilder>();


            var allProps = entityDefinition.SelectToken("$.attributes").OfType<JProperty>().Select(attributeDefinition => attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "")).ToArray();

            var acceptableBasesClass = options.DTOBaseClasses.Concat(new[] { typeof(DynamicEntity) }).Where(c => c.GetProperties().Select(p => p.Name).All(p => allProps.Contains(p)))
                .OrderByDescending(c=>c.GetProperties().Length)
                .First();

          


            var entityLogicalName = entityDefinition.SelectToken("$.logicalName").ToString();
            
           // TypeBuilder entityType =

           // options.EntityDTOsBuilders[entityCollectionSchemaName] = entityType;

            TypeBuilder entityType = options.EntityDTOsBuilders.GetOrAdd(entitySchameName, _ => myModule.DefineType($"{options.Namespace}.{entitySchameName}", TypeAttributes.Public
                                                            | TypeAttributes.Class
                                                            | TypeAttributes.AutoClass
                                                            | TypeAttributes.AnsiClass
                                                            | TypeAttributes.Serializable
                                                            | TypeAttributes.BeforeFieldInit, acceptableBasesClass));

            entityType.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { 
                typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) ,
                  typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.SchemaName)),
                    typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.CollectionSchemaName))
            }, new[] {
                entityDefinition.SelectToken("$.logicalName").ToString() ,
                 entityDefinition.SelectToken("$.schemaName").ToString(),
                  entityDefinition.SelectToken("$.collectionSchemaName").ToString()
            }));
            entityType.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntityDTOAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityDTOAttribute).GetProperty(nameof(EntityDTOAttribute.LogicalName)) }, new[] { entityDefinition.SelectToken("$.logicalName").ToString() }));
             
          
          //  var propertyChangedMethod = options.EntityBaseClass.GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
            {
                var attributeSchemaName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");
                
                
                var clrType = GetCLRType(attributeDefinition.Value, out var manifestType);
                var isprimaryKey = attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;

               
                if(acceptableBasesClass.GetProperties().Any(p=>p.Name== attributeSchemaName))
                {
                    continue;
                }


 

                //PrimaryKeys cant be null, remove nullable
                if (isprimaryKey )
                {
                    clrType = Nullable.GetUnderlyingType(clrType) ?? clrType;  
                } 

                if (clrType != null)
                {
                   
                    var (attProp, attField) = CreateProperty(entityType, attributeSchemaName, clrType);


                    if (manifestType == "lookup")
                    {
                        var FKLogicalName = attributeDefinition.Value.SelectToken("$.logicalName").ToString();
                       
                        if (FKLogicalName.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                            FKLogicalName = FKLogicalName.Substring(0, FKLogicalName.Length - 2);

                        var FKSchemaName = attributeDefinition.Value.SelectToken("$.schemaName").ToString();
                        if (FKSchemaName.EndsWith("id",StringComparison.OrdinalIgnoreCase))
                            FKSchemaName = FKSchemaName.Substring(0, FKSchemaName.Length - 2);


                        var foreigh = manifest.SelectToken($"$.entities['{attributeDefinition.Value.SelectToken("$.type.referenceType").ToString()}']");
                        //  name= foreigh.SelectToken("$.pluralName")?.ToString()

                        var foreighEntityCollectionSchemaName = (foreigh.SelectToken("$.pluralName")?.ToString() ?? (foreigh.Parent as JProperty).Name).Replace(" ", "");

                        try
                        {
                            var (attFKProp, attFKField) = CreateProperty(entityType,( FKSchemaName ??
                                (foreigh.Parent as JProperty).Name).Replace(" ", ""), foreighEntityCollectionSchemaName == entityCollectionSchemaName ? entityType : options.EntityDTOs[foreighEntityCollectionSchemaName]);

                      
                        CustomAttributeBuilder ForeignKeyAttributeBuilder = new CustomAttributeBuilder(options.ForeignKeyAttributeCtor, new object[] { attProp.Name });

                        attFKProp.SetCustomAttribute(ForeignKeyAttributeBuilder);

                        CreateJsonSerializationAttribute(attributeDefinition.Value, attFKProp, FKLogicalName);
                        CreateDataMemberAttribute(attributeDefinition.Value, attFKProp, FKLogicalName);
                     
                        }
                        catch (Exception ex)
                        {
                            File.AppendAllLines("err.txt",new[] { $"Faiiled for {entitySchameName}.{attributeSchemaName} with {foreighEntityCollectionSchemaName}" });
                           
                            throw;
                        }

                    }





                    options.OnDTOTypeGeneration?.Invoke(attributeDefinition.Value, attProp);

                    CreateDataMemberAttribute(attributeDefinition.Value, attProp, attributeDefinition.Value.SelectToken("$.logicalName").ToString());

                    CreateJsonSerializationAttribute(attributeDefinition.Value, attProp, attributeDefinition.Value.SelectToken("$.logicalName").ToString());


                    //ConfigureMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                    //ConfigureMethodIL.Emit(OpCodes.Ldstr, attProp.Name); //Constant

                    //ConfigureMethodIL.Emit(OpCodes.Callvirt, propertyMethod);

                    //ConfigureMethodIL.Emit(OpCodes.Pop);


                }

            }

            foreach(var entity in manifest.SelectToken("$.entities").OfType<JProperty>()){
                foreach(var attribute in entity.Value.SelectToken("$.attributes").OfType<JProperty>())
                {
                    var type = attribute.Value.SelectToken("$.type.referenceType")?.ToString();
                    if (type != null && manifest.SelectToken($"$.entities['{type}'].logicalName")?.ToString() == entityDefinition.SelectToken("$.logicalName").ToString())
                    {
                        File.AppendAllLines("test1.txt", new[] { $"{entity.Value.SelectToken("$.collectionSchemaName")?.ToString()} in {string.Join(",", options.EntityDTOsBuilders.Keys)}" });

                       
                        TypeBuilder related = options.EntityDTOsBuilders.GetOrAdd(entity.Name.Replace(" ", ""), _ => myModule.DefineType($"{options.Namespace}.{_}", TypeAttributes.Public
                                                           | TypeAttributes.Class
                                                           | TypeAttributes.AutoClass
                                                           | TypeAttributes.AnsiClass
                                                           | TypeAttributes.Serializable
                                                           | TypeAttributes.BeforeFieldInit, acceptableBasesClass));

                      //  if (options.EntityDTOsBuilders.ContainsKey(entity.Value.SelectToken("$.collectionSchemaName")?.ToString()))
                        {
                            //
                            var (attProp, attField) = CreateProperty(entityType, entity.Value.SelectToken("$.collectionSchemaName")?.ToString(), typeof(ICollection<>).MakeGenericType(related));
                               // methodAttributes: MethodAttributes.Virtual| MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig);
                        }
                    }
                }
            }

            // ConfigureMethodIL.Emit(OpCodes.Ret);
           

            options.EntityDTOs[entityCollectionSchemaName] = options.DTOAssembly?.GetTypes().FirstOrDefault(t=>t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute attr && attr.LogicalName == entityLogicalName)?.GetTypeInfo() ??  entityType.CreateTypeInfo();










        }

      
        private void CreateJsonSerializationAttribute(JToken value, PropertyBuilder attProp, string name)
        {
            CustomAttributeBuilder JsonPropertyAttributeBuilder = new CustomAttributeBuilder(options.JsonPropertyAttributeCtor, new object[] { name });
            CustomAttributeBuilder JsonPropertyNameAttributeBuilder = new CustomAttributeBuilder(options.JsonPropertyNameAttributeCtor, new object[] { name });

            attProp.SetCustomAttribute(JsonPropertyAttributeBuilder);
            attProp.SetCustomAttribute(JsonPropertyNameAttributeBuilder);
        }

        static ConstructorInfo DataMemberAttributeCtor = typeof(DataMemberAttribute).GetConstructor(new Type[] { });
        static PropertyInfo DataMemberAttributeNameProperty = typeof(DataMemberAttribute).GetProperty("Name");
        

        public virtual void CreateDataMemberAttribute(JToken value, PropertyBuilder attProp, string name)
        {

            CustomAttributeBuilder DataMemberAttributeBuilder = new CustomAttributeBuilder(DataMemberAttributeCtor, new object[] { }, new[] { DataMemberAttributeNameProperty }, new[] { name });

            attProp.SetCustomAttribute(DataMemberAttributeBuilder);

        }

        private (Type, ConstructorBuilder, Dictionary<string, PropertyBuilder>) CreateColumnsType(JToken manifest, string entitySchameName, string entityCollectionSchemaName, JObject entityDefinition, ModuleBuilder myModule)
        {

            var members = new Dictionary<string, PropertyBuilder>();
            TypeBuilder entityType =
                myModule.DefineType($"{options.Namespace}.{entitySchameName}Columns", TypeAttributes.Public);

            CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityAttribute).GetConstructor(new Type[] { }), new object[] { }, new[] { typeof(EntityAttribute).GetProperty(nameof(EntityAttribute.LogicalName)) }, new[] { entityDefinition.SelectToken("$.logicalName").ToString() });
            entityType.SetCustomAttribute(EntityAttributeBuilder);
            


            var dfc = entityType.DefineDefaultConstructor(MethodAttributes.Public);

            ConstructorBuilder entityCtorBuilder =
                  entityType.DefineConstructor(MethodAttributes.Public,
                                     CallingConventions.Standard, new[] {options.ColumnsBuilderType });
          


            ILGenerator entityCtorBuilderIL = entityCtorBuilder.GetILGenerator();

            entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityCtorBuilderIL.Emit(OpCodes.Call, dfc);



            foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
            {
                var attributeSchemaName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");



             


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


                var method = GetColumnForType(type);
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
                        switch (argName)
                        {
                            case "nullable":
                                var a = ((attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)) ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1;
                                entityCtorBuilderIL.Emit(a);
                                break;
                            case "type" when type == "multilinetext":
                                entityCtorBuilderIL.Emit(OpCodes.Ldstr, "nvarchar(max)");
                                break;

                            case "type" when type == "text" && typeObj.Type != JTokenType.Object:
                            case "type" when type == "string" && typeObj.Type != JTokenType.Object:
                                entityCtorBuilderIL.Emit(OpCodes.Ldstr, $"nvarchar({((attributeDefinition.Value.SelectToken("$.isPrimaryField")?.ToObject<bool>() ?? false) ? 255 : 100)})");
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

                entityCtorBuilderIL.Emit(OpCodes.Callvirt, method);
                entityCtorBuilderIL.Emit(OpCodes.Call, attProp.SetMethod);

                members[attributeDefinition.Name] = attProp;

                //  entityCtorBuilderIL

            }
            entityCtorBuilderIL.Emit(OpCodes.Ret);

            var entityClrType = entityType.CreateTypeInfo();
            return (entityClrType, entityCtorBuilder, members);
        }

        private MethodInfo GetColumnForType(string manifestType)
        {

            var baseMethodType = options.ColumnsBuilderColumnMethod;

            var type = GetCLRType(manifestType);
            if (type != null)
                return baseMethodType.MakeGenericMethod(type);

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
