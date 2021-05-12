using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Microsoft.OData.ModelBuilder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using PropertyBuilder = System.Reflection.Emit.PropertyBuilder;

namespace DotNetDevOps.Extensions.EAVFramwork
{

    public class SamplePocoImplemented
    {

         
        [DataMember(Name="name")]
        public string Name { get; set; }
    }
    public interface IMigrationManager
    {
        Dictionary<string, (Type dto, Type config)> EntityDTOs { get; }
        public Dictionary<string, Migration> BuildMigrations(string migrationName, JToken manifest, DynamicContextOptions options);

    } 
    public class MigrationManager: IMigrationManager
    {
        public Dictionary<string, (Type dto, Type config)> EntityDTOs { get; } = new Dictionary<string, (Type dto, Type config)>(StringComparer.OrdinalIgnoreCase);
        public Assembly Assembly { get; set; }
        private ConcurrentDictionary<string, Migration> _migrations = new ConcurrentDictionary<string, Migration>();
        public Dictionary<string, Migration> BuildMigrations(string migrationName, JToken manifest, DynamicContextOptions options)
        {


            AppDomain myDomain = AppDomain.CurrentDomain;
            AssemblyName myAsmName = new AssemblyName(options.Namespace);

            var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
              AssemblyBuilderAccess.RunAndCollect);
           


            ModuleBuilder myModule =
              builder.DefineDynamicModule(options.Namespace + ".dll");


            var migration= _migrations.GetOrAdd(migrationName, (migrationName) =>
             {

                 TypeBuilder migrationType =
                         myModule.DefineType($"{options.Namespace}.Migration{migrationName}", TypeAttributes.Public, typeof(DynamicMigration));


                 var ci = typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) });
                 var attributeBuilder = new CustomAttributeBuilder(ci, new object[] { migrationName });
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

                 var tables = manifest.SelectToken("$.entities").OfType<JProperty>().Select(entity => BuildEntityDefinition(myModule, manifest, entity, options)).ToArray();

                 Assembly = builder;

                 var type = migrationType.CreateType();
                 return Activator.CreateInstance(type, manifest, tables) as Migration;
             });

            return new Dictionary<string, Migration>
            {
                [migration.GetId()] = migration
            };

        }

        public IDynamicTable BuildEntityDefinition(ModuleBuilder builder, JToken manifest, JProperty entityDefinition, DynamicContextOptions options)
        {
            var EntitySchameName = entityDefinition.Name.Replace(" ", "");
            var EntityCollectionSchemaName = (entityDefinition.Value.SelectToken("$.pluralName")?.ToString() ?? EntitySchameName).Replace(" ", "");
            //   AppDomain myDomain = AppDomain.CurrentDomain;
            //   AssemblyName myAsmName = new AssemblyName("MigrationTable" + entity.Name + "Assembly");

            //  var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
            //    AssemblyBuilderAccess.Run);



            //  ModuleBuilder myModule =
            //    builder.DefineDynamicModule(myAsmName.Name);

            CreateDTO(builder, options, EntitySchameName, entityDefinition.Value as JObject);

            var (columnsCLRType, columnsctor, members) = CreateColumnsType(manifest, EntitySchameName, EntityCollectionSchemaName, entityDefinition.Value as JObject, builder);

            //   var test = Activator.CreateInstance(entityClrType,new[] { new ColumnsBuilder(new CreateTableOperation()) });

            TypeBuilder entityTypeBuilder =
             builder.DefineType(EntityCollectionSchemaName + "Builder", TypeAttributes.Public);

            var interfaceType = typeof(IDynamicTable);
            entityTypeBuilder.AddInterfaceImplementation(interfaceType);

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




            var columsMethod = entityTypeBuilder.DefineMethod("Columns", MethodAttributes.Public, columnsCLRType, new[] { typeof(ColumnsBuilder) });

            var columsMethodIL = columsMethod.GetILGenerator();
            columsMethodIL.Emit(OpCodes.Ldarg_1);
            columsMethodIL.Emit(OpCodes.Newobj, columnsctor);
            columsMethodIL.Emit(OpCodes.Ret);


            var ConstraintsMethod = entityTypeBuilder.DefineMethod("Constraints", MethodAttributes.Public, null, new[] { typeof(CreateTableBuilder<>).MakeGenericType(columnsCLRType) });
            var ConstraintsMethodIL = ConstraintsMethod.GetILGenerator();

            var primaryKeys = entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
                .Where(attribute => attribute.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)
                .Select(attribute => members[attribute.Name].GetMethod)
                .ToArray();


            var fKeys = entityDefinition.Value.SelectToken("$.attributes").OfType<JProperty>()
               .Where(attribute => attribute.Value.SelectToken("$.type.type")?.ToString() == "lookup")
               .Select(attribute => new { members[attribute.Name].GetMethod, Entity = attribute.Value.SelectToken("$.type.referenceType").ToString() })
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

                var createTableMethod = typeof(CreateTableBuilder<>).MakeGenericType(columnsCLRType).GetMethod(nameof(CreateTableBuilder<object>.PrimaryKey), BindingFlags.Public | BindingFlags.Instance, null,
                    new[] { typeof(string), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(columnsCLRType, typeof(object))) }, null);
                ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                ConstraintsMethodIL.Emit(OpCodes.Pop);
            }


            if (fKeys.Any())
            {
                foreach (var fk in fKeys.GroupBy(c => c.Entity))
                {


                    ConstraintsMethodIL.Emit(OpCodes.Ldarg_1); //first argument                    
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, $"FK_{EntityCollectionSchemaName}_{manifest.SelectToken($"$.entities['{fk.Key}'].pluralName")}");

                    WriteLambdaExpression(builder, ConstraintsMethodIL, columnsCLRType, fk.Select(c => c.GetMethod).ToArray());

                    var createTableMethod = typeof(CreateTableBuilder<>).MakeGenericType(columnsCLRType)
                        .GetMethod(nameof(CreateTableBuilder<object>.ForeignKey), BindingFlags.Public | BindingFlags.Instance, null,
                            new[] {
                                typeof(string),
                                typeof(Expression<>).MakeGenericType(
                                    typeof(Func<,>).MakeGenericType(columnsCLRType, typeof(object))),
                                typeof(string),typeof(string),typeof(string),
                                typeof(ReferentialAction),typeof(ReferentialAction) }, null);

                    var principalSchema = manifest.SelectToken($"$.entities['{fk.Key}'].schema")?.ToString() ?? options.PublisherPrefix ?? "dbo";
                    var principalTable = manifest.SelectToken($"$.entities['{fk.Key}'].pluralName").ToString().Replace(" ", "");
                    var principalColumn = manifest.SelectToken($"$.entities['{fk.Key}'].attributes").OfType<JProperty>()
                        .Single(a => a.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false);

                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalTable);
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalColumn.Name.Replace(" ", ""));
                    ConstraintsMethodIL.Emit(OpCodes.Ldstr, principalSchema);

                    ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, (int)ReferentialAction.NoAction);
                    ConstraintsMethodIL.Emit(OpCodes.Ldc_I4, (int)ReferentialAction.NoAction);


                    //
                    //onupdate
                    //ondelete
                    ConstraintsMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
                    ConstraintsMethodIL.Emit(OpCodes.Pop);
                }
            }



            ConstraintsMethodIL.Emit(OpCodes.Ret);

            var schema = entityDefinition.Value.SelectToken("$.schama")?.ToString() ?? options.PublisherPrefix ?? "dbo";

            var UpMethod = entityTypeBuilder.DefineMethod(nameof(IDynamicTable.Up), MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { typeof(MigrationBuilder) });

            var UpMethodIL = UpMethod.GetILGenerator();

            CreateTableImpl(EntityCollectionSchemaName, schema, columnsCLRType, columsMethod, ConstraintsMethod, UpMethodIL);

            UpMethodIL.Emit(OpCodes.Ret);


            var DropTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable));
            var DownMethod = entityTypeBuilder.DefineMethod(nameof(IDynamicTable.Down), MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { typeof(MigrationBuilder) });
            var DownMethodIL = DownMethod.GetILGenerator();
            DownMethodIL.Emit(OpCodes.Ldarg_1); //first argument
            DownMethodIL.Emit(OpCodes.Ldstr, EntityCollectionSchemaName); //Constant
            DownMethodIL.Emit(OpCodes.Ldstr, schema);
            DownMethodIL.Emit(OpCodes.Callvirt, DropTable);
            DownMethodIL.Emit(OpCodes.Pop);
            DownMethodIL.Emit(OpCodes.Ret);

            var type = entityTypeBuilder.CreateType();
            return Activator.CreateInstance(type) as IDynamicTable;
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

            var LambdaBase = typeof(Expression).GetMethod(nameof(Expression.Lambda), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null);
            var Lambda = LambdaBase.MakeGenericMethod(typeof(Func<,>).MakeGenericType(clrType, typeof(object)));

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
                var compositeKeyType = compositeKeyBuilder.CreateType();


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

        private static void CreateTableImpl(string entityCollectionName, string schema, Type columnsCLRType, MethodBuilder columsMethod, MethodBuilder ConstraintsMethod, ILGenerator UpMethodIL)
        {
            var createTableMethod = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)).MakeGenericMethod(columnsCLRType);

            UpMethodIL.Emit(OpCodes.Ldarg_1); //first argument
            UpMethodIL.Emit(OpCodes.Ldstr, entityCollectionName); //Constant

            UpMethodIL.Emit(OpCodes.Ldarg_0); //this
            UpMethodIL.Emit(OpCodes.Ldftn, columsMethod);
            UpMethodIL.Emit(OpCodes.Newobj, typeof(Func<,>).MakeGenericType(typeof(ColumnsBuilder), columnsCLRType).GetConstructors().Single());

            UpMethodIL.Emit(OpCodes.Ldstr, schema);

            UpMethodIL.Emit(OpCodes.Ldarg_0); //this
            UpMethodIL.Emit(OpCodes.Ldftn, ConstraintsMethod);
            UpMethodIL.Emit(OpCodes.Newobj, typeof(Action<>).MakeGenericType(typeof(CreateTableBuilder<>).MakeGenericType(columnsCLRType)).GetConstructors().Single());

            UpMethodIL.Emit(OpCodes.Ldstr, "comment");

            UpMethodIL.Emit(OpCodes.Callvirt, createTableMethod);
            UpMethodIL.Emit(OpCodes.Pop);
        }

        public virtual Type GetCLRType(JToken attributeDefinition)
        {
            var typeObj = attributeDefinition.SelectToken("$.type");
            var type = typeObj.ToString();
            if (typeObj.Type == JTokenType.Object)
            {
                type = typeObj.SelectToken("$.type").ToString();
            }

            return GetCLRType(type);
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
            }
            return null;
        }

        public virtual void OnDTOTypeGeneration(JToken attributeDefinition, PropertyBuilder attProp)
        {
          
        }

        private void CreateDTO(ModuleBuilder myModule, DynamicContextOptions options, string entitySchameName, JObject entityDefinition)
        {
           // var members = new Dictionary<string, PropertyBuilder>();

            TypeBuilder entityType = myModule.DefineType($"{options.Namespace}.{entitySchameName}", TypeAttributes.Public
                                                            | TypeAttributes.Class
                                                            | TypeAttributes.AutoClass
                                                            | TypeAttributes.AnsiClass
                                                            | TypeAttributes.Serializable
                                                            | TypeAttributes.BeforeFieldInit, typeof(DynamicEntity));

            TypeBuilder entityTypeConfiguration = myModule.DefineType($"{options.Namespace}.{entitySchameName}Configuration", TypeAttributes.Public
                                                            | TypeAttributes.Class
                                                            | TypeAttributes.AutoClass
                                                            | TypeAttributes.AnsiClass
                                                            | TypeAttributes.Serializable
                                                            | TypeAttributes.BeforeFieldInit, null, new Type[] { typeof(IEntityTypeConfiguration) });


            //myModule.DefineType($"{entitySchameName}DTOConfiguration", TypeAttributes.Public | TypeAttributes.Class, null,
            // new Type[] { typeof(IEntityTypeConfiguration<>).MakeGenericType(entityType),typeof(IEntityTypeConfiguration) }
            //);

          //  entityTypeConfiguration.DefineDefaultConstructor(MethodAttributes.Public);

         //   var ConfigureMethod = entityTypeConfiguration.DefineMethod(nameof(IEntityTypeConfiguration<object>.Configure), MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { typeof(EntityTypeBuilder<>).MakeGenericType(entityType) });
            var Configure2Method = entityTypeConfiguration.DefineMethod(nameof(IEntityTypeConfiguration<object>.Configure), MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, null, new[] { typeof(EntityTypeBuilder) });

          //  var ConfigureMethodIL = ConfigureMethod.GetILGenerator();
            var ConfigureMethod2IL = Configure2Method.GetILGenerator();


            var propertyMethod = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property),0, new[] { typeof(string) });
            var totable = typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), 0, new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) });
            var haskey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), 0, new[] { typeof(string[])});

            ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
            ConfigureMethod2IL.Emit(OpCodes.Ldstr, entityDefinition.SelectToken("$.pluralName").ToString().Replace(" ","")); //Constant
            ConfigureMethod2IL.Emit(OpCodes.Ldstr, entityDefinition.SelectToken("$.schama")?.ToString() ?? options.PublisherPrefix ?? "dbo"); //Constant

            ConfigureMethod2IL.Emit(OpCodes.Call, totable);

            ConfigureMethod2IL.Emit(OpCodes.Pop);

            var propertyChangedMethod = typeof(DynamicEntity).GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var attributeDefinition in entityDefinition.SelectToken("$.attributes").OfType<JProperty>())
            {
                var attributeSchemaName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ", "");
                 

                
                var clrType = GetCLRType(attributeDefinition.Value);
                var isprimaryKey = attributeDefinition.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false;

                //PrimaryKeys cant be null, remove nullable
                if (isprimaryKey)
                {
                    clrType = Nullable.GetUnderlyingType(clrType) ?? clrType;

                   

                }

                
                if (clrType != null )
                {
                    var (attProp, attField) = CreateProperty(entityType, attributeSchemaName, clrType);


                    if (isprimaryKey)
                    {
                        
                        ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
                        ConfigureMethod2IL.Emit(OpCodes.Ldc_I4_1); // Array length
                        ConfigureMethod2IL.Emit(OpCodes.Newarr,typeof(string));
                        ConfigureMethod2IL.Emit(OpCodes.Dup);
                        ConfigureMethod2IL.Emit(OpCodes.Ldc_I4_0);
                        ConfigureMethod2IL.Emit(OpCodes.Ldstr, attProp.Name);
                        ConfigureMethod2IL.Emit(OpCodes.Stelem_Ref); 
                        ConfigureMethod2IL.Emit(OpCodes.Callvirt, haskey);
                        ConfigureMethod2IL.Emit(OpCodes.Pop);

                    }


                    OnDTOTypeGeneration(attributeDefinition.Value, attProp);

                    CreateDataMemberAttribute(attributeDefinition.Value, attProp);

                     
                    //ConfigureMethodIL.Emit(OpCodes.Ldarg_1); //first argument
                    //ConfigureMethodIL.Emit(OpCodes.Ldstr, attProp.Name); //Constant

                    //ConfigureMethodIL.Emit(OpCodes.Callvirt, propertyMethod);

                    //ConfigureMethodIL.Emit(OpCodes.Pop);

                    ConfigureMethod2IL.Emit(OpCodes.Ldarg_1); //first argument
                    ConfigureMethod2IL.Emit(OpCodes.Ldstr, attProp.Name); //Constant

                    ConfigureMethod2IL.Emit(OpCodes.Callvirt, propertyMethod);

                    ConfigureMethod2IL.Emit(OpCodes.Pop);

                }

            }

           // ConfigureMethodIL.Emit(OpCodes.Ret);
            ConfigureMethod2IL.Emit(OpCodes.Ret);

            EntityDTOs[entitySchameName] = (entityType.CreateType(), entityTypeConfiguration.CreateType());










        }


       
        static ConstructorInfo DataMemberAttributeCtor = typeof(DataMemberAttribute).GetConstructor(new Type[] { });
        static PropertyInfo DataMemberAttributeNameProperty = typeof(DataMemberAttribute).GetProperty("Name");

        public virtual void CreateDataMemberAttribute(JToken value, PropertyBuilder attProp)
        {

            CustomAttributeBuilder DataMemberAttributeBuilder = new CustomAttributeBuilder(DataMemberAttributeCtor, new object[] {  },new[] { DataMemberAttributeNameProperty },new[] { value.SelectToken("$.logicalName").ToString() });
           
            attProp.SetCustomAttribute(DataMemberAttributeBuilder);

        }

        private (Type, ConstructorBuilder, Dictionary<string, PropertyBuilder>) CreateColumnsType(JToken manifest, string entitySchameName, string entityCollectionSchemaName, JObject entity, ModuleBuilder myModule)
        {

            var members = new Dictionary<string, PropertyBuilder>();
            TypeBuilder entityType =
                myModule.DefineType(entitySchameName, TypeAttributes.Public);



            var dfc = entityType.DefineDefaultConstructor(MethodAttributes.Public);

            ConstructorBuilder entityCtorBuilder =
                  entityType.DefineConstructor(MethodAttributes.Public,
                                     CallingConventions.Standard, new[] { typeof(ColumnsBuilder) });


            ILGenerator entityCtorBuilderIL = entityCtorBuilder.GetILGenerator();

            entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
            entityCtorBuilderIL.Emit(OpCodes.Call, dfc);



            foreach (var attributeDefinition in entity.SelectToken("$.attributes").OfType<JProperty>())
            {
                var attributeSchemaName = attributeDefinition.Value.SelectToken("$.schemaName")?.ToString() ?? attributeDefinition.Name.Replace(" ","");


                var (attProp, attField) = CreateProperty(entityType, attributeSchemaName, typeof(OperationBuilder<AddColumnOperation>));

                entityCtorBuilderIL.Emit(OpCodes.Ldarg_0);
                entityCtorBuilderIL.Emit(OpCodes.Ldarg_1);

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

                    }
                }


                var method = GetColumnForType(type);

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
                    if (argName == "type")
                        argName = "dbtype";

                    if (typeObj.Type == JTokenType.Object && typeObj[argName] is JToken token)
                    {
                        if (token.Type == JTokenType.String)
                        {
                            entityCtorBuilderIL.Emit(OpCodes.Ldstr, token.ToString());
                        }
                        else if (token.Type == JTokenType.Integer)
                        {
                            entityCtorBuilderIL.Emit(OpCodes.Ldc_I4, token.ToObject<int>());
                            if (Nullable.GetUnderlyingType(arg1.ParameterType) != null)
                            {
                                entityCtorBuilderIL.Emit(OpCodes.Newobj, arg1.ParameterType.GetConstructor(new[] { Nullable.GetUnderlyingType(arg1.ParameterType) }));
                                // It's nullable
                            }
                        }
                        else if (token.Type == JTokenType.Boolean)
                        {
                            entityCtorBuilderIL.Emit(token.ToObject<bool>() ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
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
                            case "dbtype" when type == "multilinetext":
                                entityCtorBuilderIL.Emit(OpCodes.Ldstr, "nvarchar(max)");
                                break;

                            case "dbtype" when type == "text":
                            case "dbtype" when type == "string":
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

            var entityClrType = entityType.CreateType();
            return (entityClrType, entityCtorBuilder, members);
        }

        private MethodInfo GetColumnForType(string manifestType)
        {

            var baseMethodType = typeof(ColumnsBuilder).GetMethod(nameof(ColumnsBuilder.Column), BindingFlags.Public | BindingFlags.Instance);

            var type = GetCLRType(manifestType);
            if(type!=null)
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

            return baseMethodType;
        }

        private (PropertyBuilder, FieldBuilder) CreateProperty(TypeBuilder entityTypeBuilder, string name, Type type, PropertyAttributes props = PropertyAttributes.None,
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
