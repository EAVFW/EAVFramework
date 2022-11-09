using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Security.AccessControl;

namespace EAVFramework.Shared.V2
{
    public static class TypeExtensions
    {
        public static bool IsPendingTypeBuilder(this Type foo)
        {
            return (foo is TypeBuilder) && !((foo as TypeBuilder).IsCreated());
        }
    }
    public class DefaultEmitPropertyService : IEmitPropertyService
    {
        static ConstructorInfo DataMemberAttributeCtor = typeof(DataMemberAttribute).GetConstructor(new Type[] { });
        static ConstructorInfo EntityFieldAttributeCtor = typeof(EntityFieldAttribute).GetConstructor(new Type[] { });
        static PropertyInfo DataMemberAttributeNameProperty = typeof(DataMemberAttribute).GetProperty("Name");
        static PropertyInfo EntityFieldAttributeAttributeKeyName = typeof(EntityFieldAttribute).GetProperty(nameof(EntityFieldAttribute.AttributeKey));
        protected readonly CodeGenerationOptions options;

        public DefaultEmitPropertyService(CodeGenerationOptions codeGenerationOptions)
        {
            this.options = codeGenerationOptions;
        }

        public virtual void CreateDataMemberAttribute(PropertyBuilder attProp, string logicalName, string attributeKey)
        {
            {
                CustomAttributeBuilder DataMemberAttributeBuilder =
                    new CustomAttributeBuilder(DataMemberAttributeCtor, new object[] { }, new[] { DataMemberAttributeNameProperty }, new[] { logicalName });

                attProp.SetCustomAttribute(DataMemberAttributeBuilder);
            }
            {
                CustomAttributeBuilder EntityFieldAttributeBuilder =
                   new CustomAttributeBuilder(EntityFieldAttributeCtor, new object[] { }, new[] {
                   EntityFieldAttributeAttributeKeyName
                   }, new[] { attributeKey });


                attProp.SetCustomAttribute(EntityFieldAttributeBuilder);
            }

        }
        public void CreateTableImpl(string entityCollectionName, string schema, Type columnsCLRType, MethodBuilder columsMethod, MethodBuilder ConstraintsMethod, ILGenerator UpMethodIL)
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

        public void AddForeignKey(string EntityCollectionSchemaName, string schema, ILGenerator UpMethodIL, DynamicPropertyBuilder dynamicPropertyBuilder)
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

            var principalSchema = dynamicPropertyBuilder.ReferenceType.Schema; // manifest.SelectToken($"$.entities['{entityName}'].schema")?.ToString() ?? options.Schema ?? "dbo";
            var principalTable = dynamicPropertyBuilder.ReferenceType.CollectionSchemaName;// manifest.SelectToken($"$.entities['{entityName}'].pluralName").ToString().Replace(" ", "");
            var principalColumn = dynamicPropertyBuilder.ReferenceType.Properties.Single(p => p.IsPrimaryKey).SchemaName; //  manifest.SelectToken($"$.entities['{entityName}'].attributes").OfType<JProperty>()
               // .Single(a => a.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false).Name.Replace(" ", "");

            var onDeleteCascade = dynamicPropertyBuilder.OnDeleteCascade  ?? options.ReferentialActionNoAction;
            var onUpdateCascade = dynamicPropertyBuilder .OnUpdateCascade ?? options.ReferentialActionNoAction;

            foreach (var arg1 in options.MigrationsBuilderAddForeignKey.GetParameters())
            {
                var argName = arg1.Name.ToLower();

                switch (argName)
                {
                    case "table" when !string.IsNullOrEmpty(EntityCollectionSchemaName): UpMethodIL.Emit(OpCodes.Ldstr, EntityCollectionSchemaName); break;
                    case "schema" when !string.IsNullOrEmpty(schema): UpMethodIL.Emit(OpCodes.Ldstr, schema); break;
                    case "name": UpMethodIL.Emit(OpCodes.Ldstr, $"FK_{EntityCollectionSchemaName}_{dynamicPropertyBuilder.ReferenceType.CollectionSchemaName}_{dynamicPropertyBuilder.SchemaName}".Replace(" ", "")); break;
                    case "column": UpMethodIL.Emit(OpCodes.Ldstr, dynamicPropertyBuilder.SchemaName); break;
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


            UpMethodIL.Emit(OpCodes.Callvirt, options.MigrationsBuilderAddForeignKey);
            UpMethodIL.Emit(OpCodes.Pop);
        }

        public void WriteLambdaExpression(ModuleBuilder builder, ILGenerator il, Type clrType, params MethodInfo[] getters)
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

        public void EmitNullable(ILGenerator entityCtorBuilderIL, Action p, ParameterInfo arg1)
        {

            p();//NullableContextAttribute 
            if (Nullable.GetUnderlyingType(arg1.ParameterType) != null)
            {
                entityCtorBuilderIL.Emit(OpCodes.Newobj, arg1.ParameterType.GetConstructor(new[] { Nullable.GetUnderlyingType(arg1.ParameterType) }));
                // It's nullable
            }
        }

        public virtual void AddInterfaces(DynamicTableBuilder dynamicTableBuilder)
        {
            var interfaces = options.DTOBaseInterfaces
              .Where(c => c.GetCustomAttributes<EntityInterfaceAttribute>(false).Any(attr => attr.EntityKey == dynamicTableBuilder.EntityKey ||
              (attr.EntityKey == "*")))
              .ToList();

            foreach (var @interface in interfaces)
            {
                var properties = @interface.GetCustomAttribute<CodeGenInterfacePropertiesAttribute>()?.Propeties ?? @interface.GetProperties().Select(t => t.Name).ToArray();


                if (@interface.IsGenericTypeDefinition)
                {

                    if (properties.All(c => dynamicTableBuilder.AllPropsWithLookups.Contains(c)))
                    {
                        
                        var genericArgs = @interface.GetGenericArguments().Select((c, i) => GetTypeBuilderFromConstraint(c,dynamicTableBuilder)).ToArray();

                     //   File.AppendAllLines("test1.txt", new[] { $"Generating Generic Interface: {@interface.Name}<{string.Join(",", genericArgs.Select(c=>c?.Name))}>", });
                        var a = @interface.MakeGenericType(genericArgs);

                        dynamicTableBuilder.AddInterface(a);
                        //entityInfo.Builder.AddInterfaceImplementation(a);

                        //try
                        //{
                        //    foreach (var ip in @interface.GetProperties())
                        //    {
                        //        var pp = parent;
                        //        while (!(pp == typeof(object) || pp == null))
                        //        {
                        //            if (pp.GetProperties().Any(p => p.Name == ip.Name))
                        //            {
                        //                type.DefineMethodOverride(pp.GetProperty(ip.Name).GetMethod, ip.GetMethod);
                        //            }

                        //            pp = pp.BaseType;
                        //        }

                        //    }
                        //}
                        //catch (NotSupportedException)
                        //{

                        //}


                    }

                    continue;
                }


                if (properties.All(c => dynamicTableBuilder.AllPropsWithLookups.Contains(c)))
                {
                    //  File.AppendAllLines("test1.txt", new[] { "adding " + @interface.Name + " to " + type.Name});
                    dynamicTableBuilder.AddInterface(@interface);
                }


            }
        }

        private Type GetTypeBuilderFromConstraint( Type constraint, DynamicTableBuilder dynamicTableBuilder)
        {
            try
            {
                var geneatedClass = constraint.GetGenericParameterConstraints().Where(t => !string.IsNullOrEmpty(t.GetCustomAttribute<EntityInterfaceAttribute>()?.EntityKey)).ToArray();
                if (geneatedClass.Any())
                {

                    var type = dynamicTableBuilder.AddAsDependency(GetTypeFromManifest(dynamicTableBuilder.DynamicAssemblyBuilder, geneatedClass.Single()));

                    return type?.Builder;
                }

                var found = constraint.DeclaringType.GetCustomAttributes<ConstraintMappingAttribute>().FirstOrDefault(at => at.ConstraintName == constraint.Name);
                if (found != null)
                {
                    var entitykey = found.EntityKey ?? constraint.DeclaringType.GetCustomAttribute<EntityInterfaceAttribute>().EntityKey;
                    if (string.IsNullOrEmpty(found.AttributeKey))
                    {
                        return dynamicTableBuilder.AddAsDependency( dynamicTableBuilder.DynamicAssemblyBuilder.Tables.Values.FirstOrDefault(c => c.EntityKey == entitykey)).Builder;
                    }

                    var prop = dynamicTableBuilder.DynamicAssemblyBuilder.Tables.Values.FirstOrDefault(c => c.EntityKey == entitykey).Properties
                        .FirstOrDefault(c => c.AttributeKey == found.AttributeKey);

                    if (prop == null || prop.PropertyType == null)
                    {
                        throw new InvalidOperationException($"Type was not found for Entitykey={entitykey} and AttributeKey={found.AttributeKey} for {found.ConstraintName}");
                    }

                    return Nullable.GetUnderlyingType(prop.DTOPropertyType ?? prop.PropertyType) ?? prop.DTOPropertyType ?? prop.PropertyType;

                }
            }catch(Exception ex)
            {
                throw new InvalidOperationException($"Failed to get builder from constraint: {constraint.Name}",ex);
               //throw new InvalidOperationException($"Failed to get builder from constraint: {constraint.Name} -" +
               //     $" {string.Join(",", constraint.GetGenericParameterConstraints().Select(c=>$"{c.Name}<{string.Join(",", c.GetCustomAttributes<EntityInterfaceAttribute>().Select(cc=>cc.EntityKey))}>" ))}", ex);
            }

                
            //if (!constraint.GetGenericParameterConstraints().Any())
            //{
               
            //    if (found != null)
            //    {
            //        var entitykey = found.EntityKey ?? constraint.DeclaringType.GetCustomAttribute<EntityInterfaceAttribute>().EntityKey;

            //        var prop = dynamicTableBuilder.DynamicAssemblyBuilder.Tables.Values.FirstOrDefault(c => c.EntityKey == entitykey).Properties
            //            .FirstOrDefault(c => c.AttributeKey == found.AttributeKey);

            //        if(prop==null || prop.PropertyType == null)
            //        {
            //            throw new InvalidOperationException($"Type was not found for Entitykey={entitykey} and AttributeKey={found.AttributeKey} for {found.ConstraintName}");
            //        }
            //        //if(prop.Type == "choices")
            //        //{
            //        //    prop = dynamicTableBuilder.DynamicAssemblyBuilder.Tables.Values.FirstOrDefault(c => c.EntityKey == found.AttributeKey).Properties
            //        //   .FirstOrDefault(c => c.Type =="choice");
            //        //}

            //        //var attributeDefinition = manifest.SelectToken($"$.entities['{entitykey}'].attributes['{found.AttributeKey}']");

            //        //if (attributeDefinition == null)
            //        //{
            //        //    throw new KeyNotFoundException($"Could not find {found.EntityKey}/{entitykey}/{found.AttributeKey} in manifes ");

            //        //}

            //        return Nullable.GetUnderlyingType( prop.PropertyType)?? prop.PropertyType;


            //        //var enumName = choiceEnumBuilder.GetEnumName(options, attributeDefinition);

            //        //try
            //        //{
            //        //    var t0 = CreateEnumType(myModule, attributeDefinition, enumName);
            //        //    return Nullable.GetUnderlyingType(t0) ?? t0;
            //        //    // return options.ChoiceBuilders[enumName];
            //        //}
            //        //catch (Exception ex)
            //        //{
            //        //    File.AppendAllLines("test1.txt", new[] { $"GetTypeBuilderFromConstraint: {constraint.DeclaringType.FullName} Failed={enumName}", ex.ToString() });

            //        //    throw new KeyNotFoundException($"Could not find {enumName} in {string.Join(",", options.ChoiceBuilders.Keys)}", ex);
            //        //}
            //    }
            //    else
            //    {
            //        throw new KeyNotFoundException($"Missing ConstraintMappingAttribute for {constraint.Name} on {constraint.DeclaringType.FullName}");
            //    }
            //}
            //  File.AppendAllLines("test1.txt", new[] { $"Inteface type: {constraint.DeclaringType.FullName} is Generic<{string.Join(",", constraint.GetGenericParameterConstraints().Select(p => p.Name))}>" });
            var constraints = constraint.GetGenericParameterConstraints().ToArray();


            var @interface = constraint.GetGenericParameterConstraints().Single();

            if (@interface == typeof(DynamicEntity))
                return typeof(DynamicEntity);

            return GetTypeFromManifest(dynamicTableBuilder.DynamicAssemblyBuilder,@interface)?.Builder;

        }
        private DynamicTableBuilder GetTypeFromManifest(DynamicAssemblyBuilder assembly, Type @interface)
        {
            var entityKey = @interface.GetCustomAttribute<EntityInterfaceAttribute>().EntityKey;
            if (entityKey == null)
            {
                throw new KeyNotFoundException($"Could not find entityKey on {@interface.Name}");
            }
            var schemaName = assembly.Tables.Values.Where(c => c.EntityKey == entityKey).FirstOrDefault();  // manifest.SelectToken($"$.entities['{entityKey}'].schemaName")?.ToString();

            if (schemaName == null)
            {
                throw new KeyNotFoundException($"Could not find schemaname on {entityKey}, {@interface.Name}");
            }

            return schemaName; 
        }

        public virtual void CreateJsonSerializationAttribute(PropertyBuilder attProp, string logicalName)
        {

            //[Newtonsoft.Json.JsonConverter(typeof(ChoicesConverter),typeof(AllowedGrantType), "allowedgranttype")]
            // if (!options.GeneratePoco)
            {
                CustomAttributeBuilder JsonPropertyAttributeBuilder = new CustomAttributeBuilder(options.JsonPropertyAttributeCtor, new object[] { logicalName });
                CustomAttributeBuilder JsonPropertyNameAttributeBuilder = new CustomAttributeBuilder(options.JsonPropertyNameAttributeCtor, new object[] { logicalName });


                attProp.SetCustomAttribute(JsonPropertyAttributeBuilder);
                attProp.SetCustomAttribute(JsonPropertyNameAttributeBuilder);

                //if(attributeDefinition.Value.SelectToken("$.type.type")?.ToString().ToLower() == "choices")
                //{
                //    CustomAttributeBuilder JsonConverterAttributeBuideer = new CustomAttributeBuilder(options.JsonConverterAttributeCtor, new object[] { options.ChoiceConverter, new object[] {  });

                //}
            }
        }

        public (PropertyBuilder, FieldBuilder) CreateProperty(TypeBuilder builder, string name, Type type, PropertyAttributes props = PropertyAttributes.None,
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
    }
}
