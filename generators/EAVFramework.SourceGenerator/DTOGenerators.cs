using EAVFramework;
using EAVFramework.Shared;
using EAVFramework.Shared.V2;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using static EAVFramework.Shared.TypeHelper;
using TypeInfo = System.Reflection.TypeInfo;

namespace EAVFramework.Generators
{
    public interface IDummy
    {

    }
    internal static class SourceGeneratorContextExtensions
    {
        private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

        public static string GetMSBuildProperty(
            this GeneratorExecutionContext context,
            string name,
            string defaultValue = "")
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
            return value ?? defaultValue;
        }

        public static string[] GetMSBuildItems(this GeneratorExecutionContext context, string name)
            => context
                .AdditionalFiles
                .Where(f => context.AnalyzerConfigOptions
                    .GetOptions(f)
                    .TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
                    && sourceItemGroup == name)
                .Select(f => f.Path)
                .ToArray();
    }
    public static class SortExtensions
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
    public class BaseInterfaceType
    {
        public static BaseInterfaceType Create(INamedTypeSymbol symbol) => new BaseInterfaceType { Symbol = symbol, Name = symbol.GetFullName() };
        public string Name { get; private set; }

        public INamedTypeSymbol Symbol { get; private set; }

        public IEnumerable<string> Dependencies()
        {
            if (Symbol.IsGenericType)
            {

                foreach (var ct in Symbol.TypeParameters.Select(c => c.Name).SelectMany((argument, i) =>
                                    {

                                        return Symbol.TypeParameters[i].ConstraintTypes.Select(ct =>
                                        {
                                            return ct.GetFullName();

                                        }).ToArray();



                                    }).ToArray())
                {
                    yield return ct;
                }


            }


        }
        public override string ToString()
        {
            return $"{Symbol.GetFullName()}[{string.Join(",", Dependencies())}]";
        }
    }

   

    /// <summary>
    /// https://dominikjeske.github.io/source-generators/
    /// https://github.com/dotnet/roslyn/issues/44093
    /// </summary>
    [Generator]
    public class DTOSourceGenerator : ISourceGenerator
    {

        private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
        {
            if (root != null)
            {
                foreach (var namespaceOrTypeSymbol in root.GetMembers())
                {
                    if (namespaceOrTypeSymbol is INamespaceSymbol @namespace)
                    {
                        foreach (var nested in GetAllTypes(@namespace)) { yield return nested; }
                    }

                    else if (namespaceOrTypeSymbol is INamedTypeSymbol type) yield return type;
                }
            }
        }
        public IEnumerable<INamedTypeSymbol> FindReferencedBaseTypes(GeneratorExecutionContext context, params string[] attributes)
        {

            foreach (var referencedSymbol in context.Compilation.SourceModule.ReferencedAssemblySymbols.Concat(new[] { context.Compilation.SourceModule.ContainingAssembly }))
            {
                var list = new List<INamedTypeSymbol>();
                try
                {
                    var main = referencedSymbol.Identity.Name.Split('.')?.Aggregate(referencedSymbol.GlobalNamespace, (s, c) => s.GetNamespaceMembers().SingleOrDefault(m => m.Name.Equals(c)));

                    var types = GetAllTypes(main).Where(t => t.GetAttributes().Any(n => attributes.Any(attribute =>  n.AttributeClass.Name == attribute)));

                    //File.AppendAllLines("test1.txt", new[] { 
                    //    "Refrenced Identity: "+  referencedSymbol.Identity.Name,
                    //    "Main: " + main?.Name }.Concat(types.Select(c => c.GetFullName()+ "<"+ string.Join(",",c.TypeArguments.Select(cc=>cc.Name)) + ">: "+ string.Join(",",c.GetAttributes().Select(a=>"["+a.AttributeClass.Name+"]")))));

                    list.AddRange(types);


                }
                catch (Exception ex)
                {

                }

                foreach (var type in list)
                    yield return type;
            }

        }

        public void Execute(GeneratorExecutionContext context)
        {
            //context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CustomizationPrefix", out var @namespace);
            var @schema = context.GetMSBuildProperty("CustomizationPrefix");
            var @namespace = context.GetMSBuildProperty("RootNamespace") ?? @schema ?? "EAVFramework.Extensions.Model";

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GeneratePoco", out var GeneratePoco);

            var compilation = context.Compilation;



            //  context.Compilation.GetSemanticModel(classSyntax.SyntaxTree)
            var manifest = context.AdditionalFiles.FirstOrDefault(f => Path.GetFileName(f.Path) == "manifest.g.json");
            if (manifest != null)
            {

                try
                {
                    if (!string.Equals(context.GetMSBuildProperty("EAVFrameworkSourceGenerator"), "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    var options = new CodeGenerationOptions
                    {
                   
                       
                       
                        Schema = @schema,


                        MigrationBuilderDropTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable)),
                        MigrationBuilderCreateTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)),
                        MigrationBuilderSQL = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.Sql)),
                        MigrationBuilderCreateIndex = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex), new Type[] { typeof(string), typeof(string), typeof(string[]), typeof(string), typeof(bool), typeof(string) }) ?? throw new ArgumentNullException("MigrationBuilderCreateIndex"),
                        MigrationBuilderDropIndex = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropIndex)) ?? throw new ArgumentNullException("MigrationBuilderDropIndex"),

                        MigrationsBuilderAddColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddColumn)), "MigrationsBuilderAddColumn"),
                        MigrationsBuilderAddForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AddForeignKey), new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(ReferentialAction), typeof(ReferentialAction) }), "MigrationsBuilderAddForeignKey"),
                        MigrationsBuilderAlterColumn = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.AlterColumn)), "MigrationsBuilderAlterColumn"),
                        MigrationsBuilderDropForeignKey = Resolve(() => typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropForeignKey)), "MigrationsBuilderDropForeignKey"),



                        ColumnsBuilderType = typeof(ColumnsBuilder),
                        CreateTableBuilderType = typeof(CreateTableBuilder<>),
                        CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                        CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),

                        EntityTypeBuilderType = typeof(EntityTypeBuilder),
                        EntityTypeBuilderPropertyMethod = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), new[] { typeof(string) }),
                        EntityTypeBuilderToTable = typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }),
                        EntityTypeBuilderHasKey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), new[] { typeof(string[]) }),
                    //    EntityTypeBuilderHasAlternateKey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasAlternateKey), new[] { typeof(string[]) }),

                        ForeignKeyAttributeCtor = typeof(ForeignKeyAttribute).GetConstructor(new Type[] { typeof(string) }),
                        InverseAttributeCtor = typeof(InversePropertyAttribute).GetConstructor(new Type[] { typeof(string) }),


                        OperationBuilderAddColumnOptionType = typeof(OperationBuilder<AddColumnOperation>),
                        ColumnsBuilderColumnMethod = typeof(ColumnsBuilder).GetMethod(nameof(ColumnsBuilder.Column), BindingFlags.Public | BindingFlags.Instance),
                        LambdaBase = typeof(Expression).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(n => n.Name == nameof(Expression.Lambda) && n.IsGenericMethodDefinition && ParameterMatches(n.GetParameters(), new[] { typeof(Expression), typeof(ParameterExpression[]) })).Single(),
                        //nameof(Expression.Lambda),  BindingFlags.Public | BindingFlags.Static , null, new[] { typeof(Expression), typeof(ParameterExpression[]) }, null),


                        EntityConfigurationInterface = typeof(IEntityTypeConfiguration),
                        EntityConfigurationConfigureName = nameof(IEntityTypeConfiguration.Configure),


                        JsonPropertyNameAttributeCtor = typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).GetConstructor(new Type[] { typeof(string) }),
                        JsonPropertyAttributeCtor = typeof(JsonPropertyAttribute).GetConstructor(new Type[] { typeof(string) }),

                        DynamicTableType = typeof(IDynamicTable),
                        DynamicTableArrayType = typeof(IDynamicTable[]),

                        ReferentialActionType = typeof(ReferentialAction),
                        ReferentialActionNoAction = (int)ReferentialAction.NoAction,

                       // DynamicMigrationType = typeof(DynamicMigration),
                      //  MigrationAttributeCtor = typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) }),

                       // GeneratePoco = GeneratePoco == "true",

                        IsRequiredMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRequired)),
                        //ValueGeneratedOnUpdate = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                        //    .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.ValueGeneratedOnAddOrUpdate)),
                        IsRowVersionMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRowVersion)),
                        HasConversionMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasConversion), new Type[] { }),

                        HasPrecisionMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasPrecision), new Type[] { typeof(int), typeof(int) }),
                 
                        GeoSpatialOptions = new GeoSpatialOptions
                        {
                            PointGeomeryType = typeof(Point)
                        }
                    };











                    //File.WriteAllText("test1.txt", context.GetMSBuildProperty("EAVFrameworkSourceGenerator","Empty")+"\n 1.0.1");
                    //File.AppendAllLines("test1.txt", new[] { $"includeEAVFrameworkBaseClass {GeneratePoco}" });



                    var referencedBaseTypes = FindReferencedBaseTypes(context, "BaseEntityAttribute").ToArray();
                    var interfaces = FindReferencedBaseTypes(context, "EntityInterfaceAttribute", "ConstraintMappingAttribute")
                        .Where(c=>c.TypeKind == TypeKind.Interface)
                        .Select(BaseInterfaceType.Create)
                     .ToDictionary(k => k.Name ?? Guid.NewGuid().ToString(), v => v);

                   


                    //if (!context.Compilation.ReferencedAssemblyNames.Any(ai => ai.Name.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase)))
                    //{

                    //}

                    string text = manifest.GetText(context.CancellationToken).ToString();

                    //var ms = new MemoryStream();
                    //var tw = new StreamWriter(ms);
                    //manifest.GetText().Write(tw);
                    //tw.Flush();
                    //ms.Seek(0, SeekOrigin.Begin);
                    var json = JToken.Parse(text);

                    AppDomain myDomain = AppDomain.CurrentDomain;
                    AssemblyName myAsmName = new AssemblyName(@namespace);

                    var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
                      AssemblyBuilderAccess.RunAndCollect);

                    var propertyEmitter = new DefaultEmitPropertyService(options);


                    ModuleBuilder myModule =
                      builder.DefineDynamicModule(@namespace + ".dll");

                    var dynamicEntityBaseType = typeof(DynamicEntity);
                    // var baseType = typeof(DynamicEntity);
                    var baseTypes = new List<Type>() { dynamicEntityBaseType };

                    var baseTypeInterfaces = new ConcurrentDictionary<string, Type>() { ["EAVFramework.DynamicEntity"] = dynamicEntityBaseType };

                    // if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.DTOBaseClass", out var DTOBaseClass))
                    {


                        IEnumerable<SyntaxNode> allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
                        var baseClass = allNodes
                             .Where(d => d.IsKind(SyntaxKind.ClassDeclaration))
                             .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                             .ToArray();
                        var baseclases = allNodes
                             .Where(d => d.IsKind(SyntaxKind.ClassDeclaration))
                             //  .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                             .ToArray();
                        if (baseClass.Length != baseclases.Length)
                            throw new InvalidOperationException("The wrong dll is loaded for analysers: ask PKS");

                        //File.AppendAllLines("test1.txt", new[] { 
                        //    ( baseclases.First().GetType() == typeof( Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax)).ToString(),
                        // typeof( Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax).Assembly.Location,
                        // baseclases.First().GetType().Assembly.Location

                        // });

                        var a = string.Join(",", baseClass
                         .SelectMany(c => c.AttributeLists.DefaultIfEmpty() ?? Enumerable.Empty<AttributeListSyntax>())
                         .SelectMany(aa => aa?.Attributes.DefaultIfEmpty() ?? Enumerable.Empty<AttributeSyntax>())
                         .Select(b => b.Name?.NormalizeWhitespace()?.ToFullString() ?? "dummy"));


                        var withAtt = baseClass.Where(b =>
                        (b.AttributeLists.DefaultIfEmpty() ?? Enumerable.Empty<AttributeListSyntax>())
                            .SelectMany(aa => aa?.Attributes.DefaultIfEmpty() ?? Enumerable.Empty<AttributeSyntax>())
                      .Any(bb => bb.Name?.NormalizeWhitespace()?.ToFullString() == "BaseEntity"));


                        //File.AppendAllLines("test1.txt", new[] { string.Join(",", baseclases.GroupBy(n=>n.GetType().Assembly.Location).Select(n => n.Key + n.Count())) });

                        //File.AppendAllLines("test1.txt", new[] { baseclases.Length.ToString(), string.Join(",", baseClass.Select(n=>n.GetFullName())) });

                        //File.AppendAllLines("test1.txt", new[] { a });

                        // File.AppendAllLines("test1.txt", withAtt.Select(baseType=> baseType.GetFullName()));

                        //    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "test2", a, "", DiagnosticSeverity.Warning, true), null));

                        //var withAtt = baseClass
                        //.Where(c => c.AttributeLists.FirstOrDefault()?.Attributes.Any(aa => aa.Name.NormalizeWhitespace().ToFullString() == nameof(BaseEntityAttribute)) ?? false)
                        //.ToArray();


                        var baseTypesDict = new ConcurrentDictionary<string, Type>()
                        {
                            [dynamicEntityBaseType.FullName] = dynamicEntityBaseType
                        };

                        Type CreateTypeForBaseClass(string basefullname)
                        {
                            var baseType = withAtt.Single(c => c.GetFullName() == basefullname);
                            var model = compilation.GetSemanticModel(baseType.SyntaxTree);
                            var myClassSymbol = model.GetDeclaredSymbol(baseType);

                            var baseTypeName = $"{myClassSymbol.BaseType.ContainingNamespace.ToString()}.{myClassSymbol.BaseType.Name}";


    //                        File.AppendAllLines("test1.txt", new[] { baseType.GetFullName() + " : " + baseTypeName });

                            var basetype = baseTypesDict.GetOrAdd(baseTypeName, (fullname) => CreateTypeForBaseClass(fullname));

                            if (basetype.IsGenericType)
                            {
                                // File.AppendAllLines("test1.txt", new[] { $"{basefullname} Generic: {basetype.Name}<{basetype.GetGenericTypeDefinition().GetGenericArguments()[0].Name}>" });
                                //  basetype = basetype.MakeGenericType(basetype.GetGenericArguments().Select(t => typeof(DynamicEntity)).ToArray());
                            }

                            var entityType = myModule.DefineType(baseType.GetFullName(), TypeAttributes.Public
                                                              | TypeAttributes.Class
                                                              | TypeAttributes.AutoClass
                                                              | TypeAttributes.AnsiClass
                                                              | TypeAttributes.Serializable
                                                              | TypeAttributes.BeforeFieldInit, basetype);

                            if (myClassSymbol.TypeArguments.Any())
                            {
                                //  File.AppendAllLines("test1.txt", new[] { "typeargs", string.Join(",", myClassSymbol.TypeArguments.Select(c => c.Name)) });

                                var typeParams = entityType.DefineGenericParameters(myClassSymbol.TypeArguments.Select(c => c.Name).ToArray());

                                foreach (var argument in typeParams)
                                {
                                    argument.SetBaseTypeConstraint(dynamicEntityBaseType);
                                }

                            }

                            foreach (var property in baseType.Members.OfType<PropertyDeclarationSyntax>())
                            {
                                if ((property.AttributeLists.DefaultIfEmpty() ?? Enumerable.Empty<AttributeListSyntax>())
                                        .SelectMany(aa => aa?.Attributes.DefaultIfEmpty() ?? Enumerable.Empty<AttributeSyntax>())
                                  .Any(bb => bb.Name?.NormalizeWhitespace()?.ToFullString() == "ForeignKey"))
                                {
                                    continue;
                                }

                                // File.AppendAllLines("test1.txt", new[] { property.Identifier.ToString() });
                                propertyEmitter.CreateProperty(entityType, property.Identifier.ToString(), typeof(string));
                                //CodeGenerator.CreateProperty(entityType, property.Identifier.ToString(), typeof(string));
                            }
                            //   File.AppendAllLines("test1.txt", new[] { baseType.GetFullName() +" ok" });
                            return entityType.CreateTypeInfo();
                        }


                        Type CreateTypeForReferencedBaseClass(INamedTypeSymbol baseType)
                        {

                            var parentTypeSymbol = referencedBaseTypes.FirstOrDefault(t => t.Name == baseType.BaseType.Name);
                            var parentType = parentTypeSymbol == null ? dynamicEntityBaseType : baseTypesDict.GetOrAdd(parentTypeSymbol.GetFullName(), (fullname) => CreateTypeForReferencedBaseClass(parentTypeSymbol));

                            if (parentType.IsGenericType)
                            {
                                //     File.AppendAllLines("test1.txt", new[] { "ParentType IS Generic" });

                            }

                            var entityType = myModule.DefineType(baseType.GetFullName(), TypeAttributes.Public
                                                                | TypeAttributes.Class
                                                                | TypeAttributes.AutoClass
                                                                | TypeAttributes.AnsiClass
                                                                | TypeAttributes.Serializable
                                                                | TypeAttributes.BeforeFieldInit);




                            if (baseType.TypeArguments.Any())
                            {
                                entityType.SetParent(parentType);
                                // File.AppendAllLines("test1.txt", new[] { "CreateTypeForReferencedBaseClass: ", baseType.GetFullName() +"<"+String.Join(",", baseType.TypeArguments.Select(c => c.Name).ToArray()) +"> : "+ parentType?.FullName });
                                var typeParams = entityType.DefineGenericParameters(baseType.TypeArguments.Select(c => c.Name).ToArray());

                                foreach (var argument in typeParams)
                                {
                                    argument.SetBaseTypeConstraint(dynamicEntityBaseType);
                                }

                            }
                            else
                            {

                                entityType.SetParent(parentType.MakeGenericType(entityType));
                                //     File.AppendAllLines("test1.txt", new[] { "CreateTypeForReferencedBaseClass: ", baseType.GetFullName() + " : "+ (parentType.FullName) });

                            }

                            //   baseType.



                            foreach (var property in baseType.GetMembers().OfType<IPropertySymbol>())
                            {

                              //  if (property.GetAttributes().Any(attr => attr.AttributeClass.Name == "ForeignKeyAttribute")) { continue; }

                                // File.AppendAllLines("test1.txt", new[] { property.Identifier.ToString() });
                                propertyEmitter.CreateProperty(entityType, property.Name, typeof(string));
                            }

                            var generic = baseType.GetAttributes().Where(attr => attr.AttributeClass.Name == "GenericTypeArgumentAttribute").ToArray();

                            foreach (var attr in generic)
                            {
                                CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(GenericTypeArgumentAttribute).GetConstructor(new Type[] { }),
                                    new object[] { }, new[] {
                                        typeof(GenericTypeArgumentAttribute).GetProperty(nameof(GenericTypeArgumentAttribute.ArgumentName)),
                                          typeof(GenericTypeArgumentAttribute).GetProperty(nameof(GenericTypeArgumentAttribute.ManifestKey))
                                    },
                                    new[] {
                                        attr.NamedArguments.FirstOrDefault(c=>c.Key ==nameof(GenericTypeArgumentAttribute.ArgumentName)).Value.Value,
                                     attr.NamedArguments.FirstOrDefault(c => c.Key ==nameof(GenericTypeArgumentAttribute.ManifestKey)).Value.Value
                                     });
                                entityType.SetCustomAttribute(EntityAttributeBuilder);
                            }


                            var BaseEntityAttributes = baseType.GetAttributes().Where(attr => attr.AttributeClass.Name == "BaseEntityAttribute").ToArray();

                            foreach (var attr in BaseEntityAttributes)
                            {
                                //  File.AppendAllLines("test1.txt", new[] { $"Adding ${baseType.GetFullName()} [BaseEntity(EntityKey = \"{ attr.NamedArguments.FirstOrDefault(c => c.Key ==nameof(BaseEntityAttribute.EntityKey)).Value.Value }\")]" });

                                CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(BaseEntityAttribute).GetConstructor(new Type[] { }),
                                    new object[] { }, new[] {
                                        typeof(BaseEntityAttribute).GetProperty(nameof(BaseEntityAttribute.EntityKey)),
                                    },
                                    new[] {
                                        attr.NamedArguments.FirstOrDefault(c=>c.Key ==nameof(BaseEntityAttribute.EntityKey)).Value.Value,

                                     });
                                entityType.SetCustomAttribute(EntityAttributeBuilder);
                            }


                            //if (baseType.TypeArguments.Any())
                            //{
                            //    return entityType.CreateTypeInfo().MakeGenericType(baseType.TypeArguments.Select(c => typeof(DynamicEntity)).ToArray());
                            //}

                            return entityType.CreateTypeInfo();
                        }
                        var interfacebuilders = new ConcurrentDictionary<string, TypeBuilder>();
                        //File.AppendAllLines("test1.txt", new[] { $"All Interfaces Found: {string.Join("\n", interfaces.Values)}" });
                        //File.AppendAllLines("test1.txt", new[] { $"All Interfaces Found: {string.Join("\n", interfaces.Values.TSort(x => x.Dependencies().Where(c => interfaces.ContainsKey(c)).Select(c => interfaces[c])).Select(c => c.Name))}" });
                        var baseTypeInterfacesbuilders = new ConcurrentDictionary<string, InterfaceShadowBuilder>();

                        foreach (var @interfaceWrap in interfaces.Values.TSort(x => x.Dependencies().Where(c => interfaces.ContainsKey(c)).Select(c => interfaces[c])))
                        {
                            var @interface = @interfaceWrap.Symbol;
                          //  File.AppendAllLines("test1.txt", new[] { $"Building Inteface type: {@interface.GetFullName()}<{string.Join(",", @interface.TypeParameters.SelectMany(p => p.ConstraintTypes).Select(c => c.GetFullName() + ":" + c.ContainingAssembly.Name))}>" });
                            baseTypeInterfacesbuilders.GetOrAdd(@interface.GetFullName(), (fullname) =>
                            {
                                return new InterfaceShadowBuilder(myModule, baseTypeInterfacesbuilders, @interface.GetFullName());
                               
                            });
                        }


                        foreach (var @interfaceWrap in interfaces.Values.TSort(x => x.Dependencies().Where(c => interfaces.ContainsKey(c)).Select(c => interfaces[c])))
                        {

                            var @interface = @interfaceWrap.Symbol;
                           // File.AppendAllLines("test1.txt", new[] { $"Extending Inteface type: {@interface.GetFullName()}<{string.Join(",", @interface.TypeParameters.SelectMany(p => p.ConstraintTypes).Select(c => c.GetFullName() + ":" + c.ContainingAssembly.Name))}>" });

                            //  baseTypeInterfaces.GetOrAdd(@interface.GetFullName(), (fullname) =>
                            {

                                //TypeBuilder interfaceEntityType = myModule.DefineType(@interface.GetFullName(), TypeAttributes.Public
                                //                              | TypeAttributes.Interface
                                //                              | TypeAttributes.Abstract
                                //                              | TypeAttributes.AutoClass
                                //                              | TypeAttributes.AnsiClass
                                //                              | TypeAttributes.Serializable
                                //                              | TypeAttributes.BeforeFieldInit);
                                var interfaceEntityType = baseTypeInterfacesbuilders[@interface.GetFullName()];

                                if (@interface.IsGenericType)
                                {

                                    foreach(var typeparameter in @interface.TypeParameters)
                                    {
                                        var name = typeparameter.Name;
                                       // var contraints = typeparameter.ConstraintTypes.Where(ct => ct.GetFullName() != "System.IConvertible").ToArray();

                                        foreach(INamedTypeSymbol contraint in typeparameter.ConstraintTypes)
                                        {
                                            var contraintFullName = contraint.GetFullName();

                                            if(contraintFullName == "System.IConvertible")
                                            {
                                                interfaceEntityType.AddContraint(name);
                                                continue;
                                            }

                                            if (baseTypeInterfaces.ContainsKey(contraintFullName))
                                            {
                                         //       File.AppendAllLines("test1.txt", new[] { $"Creating Contraint {contraintFullName} from BaseInterfaces" });

                                                interfaceEntityType.AddContraint(name, baseTypeInterfaces[contraintFullName]);
                                                continue;
                                            }


                                            if(baseTypeInterfacesbuilders.ContainsKey(contraintFullName))
                                            {
                                                var defaultEntityKey = @interface.GetAttributes().Where(attr => attr.AttributeClass.Name == "EntityInterfaceAttribute")
                                                    .FirstOrDefault().NamedArguments.FirstOrDefault(k => k.Key == "EntityKey").Value.Value as string;

                                                var BaseEntityAttributes = @interface.GetAttributes().Where(attr => attr.AttributeClass.Name == "ConstraintMappingAttribute")

                                                    .Select(c => new
                                                    {
                                                        EntityKey = c.NamedArguments.FirstOrDefault(k => k.Key == "EntityKey").Value.Value as string ?? defaultEntityKey ?? throw new KeyNotFoundException($"ConstraintMappingAttribute EntityKey default='{defaultEntityKey}' not found for " + @interface.Name),
                                                        ConstraintName = c.NamedArguments.FirstOrDefault(k => k.Key == "ConstraintName").Value.Value as string ?? throw new KeyNotFoundException("ConstraintMappingAttribute Attribute Key not found for " + @interface.Name)
                                                    })
                                                    .ToDictionary(k=>k.ConstraintName, v=>v.EntityKey);


                                            //    File.AppendAllLines("test1.txt", new[] { $"Creating Contraint {contraintFullName}<{string.Join(",",contraint.TypeParameters.Select(tp=>tp.Name))}> from BaseInterfacesBuilders: {name}" });

                                                if (contraint.TypeParameters.Any())
                                                {
                                                    interfaceEntityType.AddContraint(name, baseTypeInterfacesbuilders[contraintFullName], contraint.TypeParameters.Select(tp=>tp.Name).ToArray());
                                                    continue;
                                                }

                                                interfaceEntityType.AddContraint(name, baseTypeInterfacesbuilders[contraintFullName]);
                                                continue;
                                            }


                                            throw new InvalidOperationException("Could not find " + contraintFullName);

                                        }
                                        
                                       


                                    }
                                    
                                    //   File.AppendAllLines("test1.txt", new[] { $"Inteface type: {@interface.GetFullName()} is Generic<{string.Join(",", @interface.TypeParameters.SelectMany(p => p.ConstraintTypes).Select(c => c.GetFullName() + ":" + c.ContainingAssembly.Name))}>" });
                                    //var entityType = interfacebuilders[@interface.GetFullName()];
                                    //var b = interfaceEntityType.Builder.DefineGenericParameters(@interface.TypeParameters.Select(c => c.Name).ToArray())
                                    //.Select((argument, i) =>
                                    //{
                                    //    argument.SetInterfaceConstraints(@interface.TypeParameters[i].ConstraintTypes.Where(ct => ct.GetFullName() != "System.IConvertible").Select(ct =>
                                    //    {
                                    //        try
                                    //        {
                                    //            File.AppendAllLines("test1.txt", new[] { $"Looking for : {ct.GetFullName()} in {string.Join(",", baseTypeInterfaces.Keys)}" });

                                    //            if (!baseTypeInterfaces.ContainsKey(ct.GetFullName()))
                                    //            {
                                    //                File.AppendAllLines("test1.txt", new[] { $"Looking for : {ct.GetFullName()} in {string.Join(",", baseTypeInterfacesbuilders.Keys)}" });

                                    //                if (!baseTypeInterfacesbuilders.ContainsKey(ct.GetFullName()))
                                    //                {

                                    //                    TypeBuilder interfaceEntityTypeShadow = myModule.DefineType(ct.GetFullName() + @interface.Name, TypeAttributes.Public
                                    //                                                  | TypeAttributes.Interface
                                    //                                                  | TypeAttributes.Abstract
                                    //                                                  | TypeAttributes.AutoClass
                                    //                                                  | TypeAttributes.AnsiClass
                                    //                                                  | TypeAttributes.Serializable
                                    //                                                  | TypeAttributes.BeforeFieldInit);

                                    //                    var constraintinterface = ct as INamedTypeSymbol;
                                    //                    File.AppendAllLines("test1.txt", new[] { $"Inteface type: {constraintinterface.GetFullName()} is Shadow<{string.Join(",", @interface.TypeParameters.Select(t => t.Name + " " + t.GetAttributes().Count() + "=" + String.Join("|", t.GetAttributes().Select(attr => attr.AttributeClass.Name + "=" + String.Join(",", attr.NamedArguments.Select(kv => $"{kv.Key}={kv.Value.ToString()}"))))))}>" });




                                    //                    AddEntityKeyAttributes(ct as INamedTypeSymbol, interfaceEntityTypeShadow);
                                    //                    //    File.AppendAllLines("test1.txt", new[] { $"Added shadow type: {interfaceEntityTypeShadow.Name}" });
                                    //                    var shadow = interfaceEntityTypeShadow.CreateTypeInfo();
                                    //                    baseTypeInterfacesbuilders[ct.GetFullName()] = interfaceEntityTypeShadow;
                                    //                    return shadow;
                                    //                }

                                    //                var constraint = baseTypeInterfacesbuilders[ct.GetFullName()];
                                    //                if (constraint.IsGenericType)
                                    //                {

                                    //                    File.AppendAllLines("test1.txt", new[] { $"{constraint.Name}<{string.Join(",", constraint.GenericTypeArguments.Select(aa => aa.FullName))}>" });
                                    //                }
                                    //                return constraint;
                                    //            }


                                    //            return baseTypeInterfaces[ct.GetFullName()];

                                    //        }
                                    //        catch (Exception ex)
                                    //        {
                                    //            //        File.AppendAllLines("test1.txt", new[] { $"[{@interface.GetFullName()}] Could not find {ct.GetFullName()} in {string.Join(",", baseTypeInterfaces.Keys)}. {ct.GetFullName() == @interface.GetFullName()}", ex.ToString()});

                                    //            throw new KeyNotFoundException($"[{@interface.GetFullName()}] Could not find {ct.GetFullName()} in {string.Join(",", baseTypeInterfaces.Keys)}. {ct.GetFullName() == @interface.GetFullName()}", ex);
                                    //        }

                                    //    }).ToArray());

                                    //    return argument;



                                    //}).ToArray();

                                   // File.AppendAllLines("test1.txt", new[] { $"Inteface type: {@interface.GetFullName()}\n{string.Join("\n",b.Select(c=> $"where {c.Name} : " ))}" });

                                }

                                CustomAttributeBuilder CodeGenInterfacePropertiesAttributeBuilder = new CustomAttributeBuilder(typeof(CodeGenInterfacePropertiesAttribute).GetConstructor(new Type[] { }),
                                     new object[] { }, new[] {
                                        typeof(CodeGenInterfacePropertiesAttribute).GetProperty(nameof(CodeGenInterfacePropertiesAttribute.Propeties)),
                                     },
                                     new[] {
                                         @interface.GetMembers().OfType<IPropertySymbol>().Select(c=>c.Name).ToArray(),

                                      });
                                interfaceEntityType.Builder.SetCustomAttribute(CodeGenInterfacePropertiesAttributeBuilder);


                                



                                //foreach (var prop in @interface.GetMembers().OfType<IPropertySymbol>())
                                //{
                                //    if (prop.Kind == SymbolKind.Property) {
                                //        CodeGenerator.CreateProperty(interfaceEntityType,prop.Name,prop.Type.)

                                //        }
                                //}

                                AddEntityKeyAttributes(@interface, interfaceEntityType.Builder);
                                // return interfaceEntityType.CreateTypeInfo();

                            }//);



                        }

                  //      File.AppendAllLines("test1.txt", new[] { $"All Builders Created: {string.Join("\n", baseTypeInterfacesbuilders.Select(c => c.Value.FullName))}" });

                        foreach (var @interfaceWrap in interfaces.Values.TSort(x => x.Dependencies().Where(c => interfaces.ContainsKey(c)).Select(c => interfaces[c])))
                        {
                    //        File.AppendAllLines("test1.txt", new[] { $"Creating {@interfaceWrap}" });
                            var @interface = @interfaceWrap.Symbol;

                            baseTypeInterfaces.GetOrAdd(@interface.GetFullName(), (fullname) => baseTypeInterfacesbuilders[@interface.GetFullName()].CreateType());
                        }
                    //    File.AppendAllLines("test1.txt", new[] { $"All Interfaces Created:\n{string.Join("\n", baseTypeInterfaces.Select(c => InterfaceShadowBuilder.DumpInterface( c.Value)))}" });
                        //foreach (var @interface in interfaces)
                        //{

                        //    if (@interface.IsGenericType)
                        //    {
                        //        File.AppendAllLines("test1.txt", new[] { $"Inteface type: {@interface.GetFullName()} is Generic<{string.Join(",", @interface.TypeParameters.SelectMany(p=>p.ConstraintTypes).Select(c=>c.GetFullName()+":"+c.ContainingAssembly.Name))}>" });
                        //        var entityType = interfacebuilders[@interface.GetFullName()];
                        //        var b = entityType.DefineGenericParameters(@interface.TypeParameters.Select(c => c.Name).ToArray())
                        //        .Select((argument, i) =>
                        //        {
                        //            argument.SetInterfaceConstraints(@interface.TypeParameters[i].ConstraintTypes.Select(ct=>interfacebuilders[ct.GetFullName()]).ToArray());
                        //            return argument;
                        //        }).ToArray();
                        //    }
                        //}

                        //foreach (var @interface in interfacebuilders)
                        //{ 
                        //    baseTypeInterfaces.TryAdd(@interface.Key, @interface.Value.CreateTypeInfo());
                        //}


                        foreach (var baseType in referencedBaseTypes)
                        {


                            baseTypes.Add(baseTypesDict.GetOrAdd(baseType.GetFullName(), (fullname) => CreateTypeForReferencedBaseClass(baseType)));
                           // File.AppendAllLines("test1.txt", new[] { $"Base type: {baseType.GetFullName()} ok" });
                            //return entityType.CreateTypeInfo();

                        }

                        foreach (var baseType in withAtt)
                        {
                           
                            baseTypes.Add(baseTypesDict.GetOrAdd(baseType.GetFullName(), (fullname) => CreateTypeForBaseClass(fullname)));
                        }





                    }

                    foreach(var basetypes in baseTypes.ToArray())
                    {
                        ;

                  //      File.AppendAllLines("test1.txt", new[] { $"Base type: {basetypes.Name}<{string.Join(",", basetypes.GetProperties().Select(c=>c.Name))}>" });
                    }
                 //   File.AppendAllLines("test1.txt", new[] { $"Generating Code from manifest" });
                    var manifestservice = new ManifestService(new ManifestServiceOptions { Namespace= @namespace, MigrationName = $"{@namespace}_{json.SelectToken("$.version") ?? "Initial"}", });
                    options.DTOBaseClasses = baseTypes.ToArray();
                    options.DTOBaseInterfaces = baseTypeInterfaces.Values.Where(c => c.IsInterface).ToArray();

                    //  var generator = new CodeGenerator();


                    // var migrationType = generator.CreateDynamicMigration(json);

                    // var tables = generator.GetTables(json, myModule);
                    var codeservice = new DynamicCodeService(options);
                    var (migrationType, tables) = manifestservice.BuildDynamicModel(codeservice, json);

                    var code = codeservice.GenerateCodeFiles();

                    foreach(var file in code)
                    {
                        context.AddSource(file.Key,file.Value);
                    }

                    //I think its here we should generate some openapi spec, looping over the entities in our model.
                    //However i would like the augment the DTO types in the code generator with some attributes that controls it,
                    //so we also can generate it at runtime dynamic for custom entites. 
                    //Same approach as the codegenerator, make a class that is "shared" by the projects and runs on top of the generated DTO classes (EACH DTO class is a endpoint after all).
                    //Remember, dont make it perfect the first time, just get it work and we can add features.

                    //)
                    //var enums = new HashSet<Type>();
                    //foreach (var type in myModule.GetTypes().Where(t => { try { return t.GetCustomAttribute<EntityDTOAttribute>() != null; } catch (Exception) { } return false; }))
                    //{

                    //    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "Generating", "Generated for " + type.GetCustomAttribute<EntityAttribute>().LogicalName, "", DiagnosticSeverity.Info, true), null));

                    //    context.AddSource($"{type.Name}.cs", GenerateSourceCode(type, GeneratePoco == "true"));

                    //    foreach (var prop in type.GetProperties().Where(p => (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType).IsEnum))
                    //    {
                    //        enums.Add(Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                    //    }



                    //}
                    //foreach (var enumtype in enums)
                    //{

                    //    context.AddSource($"{enumtype.Name}.cs", GenerateSourceCode(enumtype, GeneratePoco == "true"));
                    //}

                }
                catch (Exception ex)
                {
                    throw;
                    //try
                    //{
                    //    File.AppendAllLines("err.txt", new[] { ex.ToString() });
                    //}
                    //    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "test2", ex.StackTrace.ToString(), "", DiagnosticSeverity.Info, true), null));
                    //    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("101", "test2", ex.ToString().Replace("\n", " "), "", DiagnosticSeverity.Info, true), null));

                }

                //                //   context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "test2", json.ToString(Newtonsoft.Json.Formatting.None), "", DiagnosticSeverity.Warning, true), null));
                //                foreach (var entity in json.SelectToken("$.entities").OfType<JProperty>())
                //                {
                //                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "test2", entity.Value.SelectToken("$.schemaName").ToString(), "", DiagnosticSeverity.Warning, true), null));

                //                    context.AddSource(entity.Value.SelectToken("$.schemaName").ToString() + ".cs",
                //$@"using System;
                //namespace {@namespace} {{

                //public class {entity.Value.SelectToken("$.schemaName")} {{

                //            {string.Join("\n", entity.Value.SelectToken("$.attributes")?.OfType<JProperty>().Select(p=> GenerateProperty(json,p))?? Enumerable.Empty<string>())}

                //}}}}");

                //                }

            }


            //  context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("102", "test33", @namespace ?? "empty", "", DiagnosticSeverity.Warning, true), null));
            // context.AddSource("test.cs", "public class HelloWorld{}");
            // Console.WriteLine(test);


        }

       

        private static void AddEntityKeyAttributes(INamedTypeSymbol @interface, TypeBuilder interfaceEntityType)
        {
            var BaseEntityAttributes = @interface.GetAttributes().Where(attr => attr.AttributeClass.Name == "EntityInterfaceAttribute").ToArray();

            foreach (var attr in BaseEntityAttributes)
            {

                //   File.AppendAllLines("test1.txt", new[] { $"Adding {@interface.GetFullName()} [EntityInterfaceAttribute(EntityKey = \"{ attr.NamedArguments.FirstOrDefault(c => c.Key ==nameof(EntityInterfaceAttribute.EntityKey)).Value.Value }\")]" });

                CustomAttributeBuilder EntityAttributeBuilder = new CustomAttributeBuilder(typeof(EntityInterfaceAttribute).GetConstructor(new Type[] { }),
                  new object[] { }, new[] {
                                        typeof(EntityInterfaceAttribute).GetProperty(nameof(EntityInterfaceAttribute.EntityKey)),
                  },
                  new[] {
                                        attr.NamedArguments.FirstOrDefault(c=>c.Key ==nameof(EntityInterfaceAttribute.EntityKey)).Value.Value,

                   });
                interfaceEntityType.SetCustomAttribute(EntityAttributeBuilder);
            }

            foreach (var attr in @interface.GetAttributes().Where(n => n.AttributeClass.Name == "ConstraintMappingAttribute"))
            {
              //  File.AppendAllLines("test1.txt", new[] { $"Adding ConstraintMappingAttribute: {string.Join(",", attr.NamedArguments.Select(c=>$"{c.Key}={c.Value.Value}"))} " });

                CustomAttributeBuilder ConstraintMappingAttributeBuilder = new CustomAttributeBuilder(typeof(ConstraintMappingAttribute).GetConstructor(new Type[] { }),
                   new object[] { }, new[] {
                                                                                        typeof(ConstraintMappingAttribute).GetProperty(nameof(ConstraintMappingAttribute.AttributeKey)),
                                                                                        typeof(ConstraintMappingAttribute).GetProperty(nameof(ConstraintMappingAttribute.ConstraintName)),
                                                                                         typeof(ConstraintMappingAttribute).GetProperty(nameof(ConstraintMappingAttribute.EntityKey)),
                   },
                   new[] {
                                                                                        attr.NamedArguments.FirstOrDefault(c=>c.Key ==nameof(ConstraintMappingAttribute.AttributeKey)).Value.Value,
                                                                                        attr.NamedArguments.FirstOrDefault(c=>c.Key ==nameof(ConstraintMappingAttribute.ConstraintName)).Value.Value,
                                                                                        attr.NamedArguments.FirstOrDefault(c=>c.Key ==nameof(ConstraintMappingAttribute.EntityKey)).Value.Value,

                    });
                interfaceEntityType.SetCustomAttribute(ConstraintMappingAttributeBuilder);
            }
        }

        private string GenerateSourceCode(Type type, bool generatePoco)
        {
            var namespaces = new HashSet<string>();

            var sb = new StringBuilder();

            sb.AppendLine($"namespace {type.Namespace}\r\n{{");
            {
                var inherience = type.BaseType?.Name;
                if (type.BaseType == typeof(object))
                {
                    inherience = null;
                }
                namespaces.Add(type.BaseType.Namespace);

                if (!string.IsNullOrEmpty(inherience))
                {
                    inherience = " : " + SerializeGenericType(type.BaseType, namespaces);
                }

                Type[] allInterfaces = type.GetInterfaces();
                var exceptInheritedInterfaces = allInterfaces.Where(i => !type.BaseType.GetInterfaces().Any(i2 => i2 == i));

                foreach (var @interface in exceptInheritedInterfaces)
                {
                    inherience += ", " + SerializeGenericType(@interface, namespaces);
                }

                if (!generatePoco)
                    GenerateAttributes(sb, "\t", namespaces, type.CustomAttributes);

                if (type.IsEnum)
                {
                    sb.AppendLine($"\tpublic enum {type.Name}\r\n\t{{");
                    {
                        System.Type enumUnderlyingType = System.Enum.GetUnderlyingType(type);
                        System.Array enumValues = System.Enum.GetValues(type);
                        // System.Array enumNames = System.Enum.GetNames(type);

                        for (int i = 0; i < enumValues.Length; i++)
                        {
                            // Retrieve the value of the ith enum item.
                            object value = enumValues.GetValue(i);
                            // object name = enumNames.GetValue(i);

                            // Convert the value to its underlying type (int, byte, long, ...)
                            object underlyingValue = System.Convert.ChangeType(value, enumUnderlyingType);

                            sb.Append($"\t\t{value} = {underlyingValue}");
                            if (i + 1 < enumValues.Length)
                                sb.AppendLine(",");
                            else
                                sb.AppendLine();

                        }
                    }
                }
                else
                {
                    sb.AppendLine($"\tpublic{(false && type.IsAbstract ? " abstract " : " ")}partial class {type.Name}{inherience}\r\n\t{{");
                    {
                        foreach (var ctor in type.GetConstructors())
                        {
                            GenerateContructorSourceCode(sb, "\t\t", namespaces, ctor);
                        }
                        foreach (var method in type.GetMethods())
                        {
                            var body = method.GetMethodBody();

                        }
                        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public
        | System.Reflection.BindingFlags.Instance
        | System.Reflection.BindingFlags.DeclaredOnly))
                        {

                            GeneratePropertySource(sb, "\t\t", namespaces, prop);

                        }


                    }
                }

                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");
            return string.Join("\r\n", namespaces.Select(n => $"using {n};")) + "\r\n" + sb.ToString();
        }

        private void GenerateContructorSourceCode(StringBuilder sb, string indention, HashSet<string> namespaces, ConstructorInfo ctor)
        {
            sb.AppendLine($"{indention}public {ctor.DeclaringType.Name}({string.Join(",", ctor.GetParameters().Select(p => $"{p.ParameterType.Namespace}.{p.ParameterType.Name} {p.Name ?? "arg" + p.Position}"))})");
            sb.AppendLine($"{indention}{{");

            sb.AppendLine($"{indention}}}");
            sb.AppendLine();
        }

        private void GeneratePropertySource(StringBuilder sb, string indention, HashSet<string> namespaces, PropertyInfo prop)
        {
            try
            {
                GenerateAttributes(sb, indention, namespaces, prop.CustomAttributes);
                //{(prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>)? " virtual":"")}
                sb.AppendLine($"{indention}public {SerializeType(prop.PropertyType, namespaces)} {prop.Name} {{get;set;}}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"{indention}///public ... {prop.Name} {{get;set;}}");
                sb.AppendLine($"{indention}/// {string.Join($"\n{indention}\\\\\\", ex.Message.Split('\n'))}");
            }
            sb.AppendLine();
        }

        private void GenerateAttributes(StringBuilder sb, string indention, HashSet<string> namespaces, IEnumerable<CustomAttributeData> customAttributes)
        {
            foreach (var attr in customAttributes)
            {
                // namespaces.Add(attr.AttributeType.Namespace);
                var namedParams = string.Join(",", attr.NamedArguments.Select(p => $"{p.MemberName}={SerializeArgument(p.TypedValue.Value)}"));
                if (attr.NamedArguments.Any() && attr.ConstructorArguments.Any())
                    namedParams = "," + namedParams;
                namespaces.Add(attr.AttributeType.Namespace);

                sb.AppendLine($"{indention}[{attr.AttributeType.Name.Substring(0, attr.AttributeType.Name.IndexOf("Attribute"))}({string.Join(",", attr.ConstructorArguments.Select(p => $"{SerializeArgument(p.Value)}"))}{namedParams})]");
            }
        }

        private string SerializeType(Type propertyType, HashSet<string> namespaces)
        {
            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                propertyType = Nullable.GetUnderlyingType(propertyType);

                return $"{SerializeGenericType(propertyType, namespaces)}?";

            }


            return $"{SerializeGenericType(propertyType, namespaces)}";
        }

        private string SerializeGenericType(Type propertyType, HashSet<string> namespaces)
        {
            if (propertyType.IsGenericType)
            {
                var gen = propertyType.GetGenericTypeDefinition();
                namespaces.Add(gen.Namespace);
                return $"{gen.Name.Substring(0, gen.Name.IndexOf('`') == -1 ? gen.Name.Length : gen.Name.IndexOf('`'))}<{string.Join(",", propertyType.GenericTypeArguments.Select(t => SerializeType(t, namespaces)))}>";

            }
            namespaces.Add(propertyType.Namespace);
            return $"{propertyType.Name}";
        }

        private string SerializeArgument(object value)
        {
            if (value is string str)
            {
                return $"\"{str}\"";
            }
            else if (value is bool)
            {
                return value.ToString().ToLower();
            }
            return $"\"{value}\"";
        }

        private bool ParameterMatches(ParameterInfo[] parameterInfos, Type[] types)
        {
            if (parameterInfos.Length != types.Length)
                return false;

            for (var j = 0; j < types.Length; j++)
                if (parameterInfos[j].ParameterType != types[j])
                    return false;

            return true;
        }

        private string GenerateProperty(JToken json, JProperty p)
        {
            try
            {
                return $"public {GetType(json, p.Value, p.Value.SelectToken("$.type"))} {p.Value.SelectToken("$.schemaName")} {{get;set;}}";
            }
            catch (Exception ex)
            {
                return $"/// {p.Value.SelectToken("$.schemaName")} {{get;set;}}";
            }
        }

        private object GetType(JToken document, JToken attribute, JToken type)
        {

            if (type.Type == JTokenType.String)
                return MapType(document, attribute, type.ToString());
            return MapType(document, attribute, type.SelectToken("$.type").ToString());
        }

        private string MapType(JToken document, JToken attribute, string type)
        {

            switch (type.ToLower())
            {
                case "string":
                case "text":
                case "multilinetext":
                    return "string";
                case "int":
                case "interger":
                    return "int";
                case "datetime":
                    return "DateTimeOffset";
                case "guid":
                    return "Guid";
                case "decimal":
                    return "decimal";
            }
            throw new NotImplementedException(type);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch();
            //            }
            //#endif 
        }
    }
}
