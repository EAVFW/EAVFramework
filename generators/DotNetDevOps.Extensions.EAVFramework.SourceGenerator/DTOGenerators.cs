using DotNetDevOps.Extensions.EAVFramework;
using DotNetDevOps.Extensions.EAVFramework.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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



namespace DotNetDevOps.Extensions.EAVFramework.Generators
{

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

    /// <summary>
    /// https://dominikjeske.github.io/source-generators/
    /// https://github.com/dotnet/roslyn/issues/44093
    /// </summary>
    [Generator]
    public class DTOSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            //context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CustomizationPrefix", out var @namespace);

            var @namespace = context.GetMSBuildProperty("RootNamespace") ?? context.GetMSBuildProperty("CustomizationPrefix", "EAVFramework.Extensions.Model");

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GeneratePoco", out var GeneratePoco);

            var compilation = context.Compilation;

           
         
            //  context.Compilation.GetSemanticModel(classSyntax.SyntaxTree)
            var manifest = context.AdditionalFiles.FirstOrDefault(f => Path.GetFileName(f.Path) == "manifest.g.json");
            if (manifest != null)
            {
                
                try
                {
                    File.WriteAllText("test1.txt", context.GetMSBuildProperty("EAVFrameworkSourceGenerator","Empty")+"\n 1.0.1");
                    File.AppendAllLines("test1.txt", new[] { $"includeEAVFrameworkBaseClass {GeneratePoco}" });

                    if (!string.Equals(context.GetMSBuildProperty("EAVFrameworkSourceGenerator"),"true",StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

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



                    ModuleBuilder myModule =
                      builder.DefineDynamicModule(@namespace + ".dll");

                    // var baseType = typeof(DynamicEntity);
                    var baseTypes = new List<Type>() { typeof(DynamicEntity) };

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

                        File.AppendAllLines("test1.txt", new[] { 
                            ( baseclases.First().GetType() == typeof( Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax)).ToString(),
                         typeof( Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax).Assembly.Location,
                         baseclases.First().GetType().Assembly.Location

                        });

                        var a = string.Join(",", baseClass
                         .SelectMany(c => c.AttributeLists.DefaultIfEmpty()??Enumerable.Empty<AttributeListSyntax>())
                         .SelectMany(aa => aa?.Attributes.DefaultIfEmpty()??Enumerable.Empty<AttributeSyntax>())
                         .Select(b => b.Name?.NormalizeWhitespace()?.ToFullString()??"dummy"));


                        var withAtt =  baseClass.Where(b=> 
                        (b.AttributeLists.DefaultIfEmpty() ?? Enumerable.Empty<AttributeListSyntax>())
                            .SelectMany(aa => aa?.Attributes.DefaultIfEmpty() ?? Enumerable.Empty<AttributeSyntax>())
                      .Any(bb => bb.Name?.NormalizeWhitespace()?.ToFullString() == "BaseEntity"));


                        File.AppendAllLines("test1.txt", new[] { string.Join(",", baseclases.GroupBy(n=>n.GetType().Assembly.Location).Select(n => n.Key + n.Count())) });

                        File.AppendAllLines("test1.txt", new[] { baseclases.Length.ToString(), string.Join(",", baseClass.Select(n=>n.GetFullName())) });

                        File.AppendAllLines("test1.txt", new[] { a });
                      
                       // File.AppendAllLines("test1.txt", withAtt.Select(baseType=> baseType.GetFullName()));

                        //    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "test2", a, "", DiagnosticSeverity.Warning, true), null));

                        //var withAtt = baseClass
                        //.Where(c => c.AttributeLists.FirstOrDefault()?.Attributes.Any(aa => aa.Name.NormalizeWhitespace().ToFullString() == nameof(BaseEntityAttribute)) ?? false)
                        //.ToArray();

                        var baseTypesDict = new ConcurrentDictionary<string, Type>() { [typeof(DynamicEntity).FullName] = typeof(DynamicEntity) };

                        Type CreateTypeForBaseClass(string basefullname)
                        {
                            var baseType = withAtt.Single(c => c.GetFullName() == basefullname);
                            var model = compilation.GetSemanticModel(baseType.SyntaxTree);
                            var myClassSymbol = model.GetDeclaredSymbol(baseType);
                            if (myClassSymbol.TypeArguments.Any())
                            {
                                File.AppendAllLines("test1.txt", new[] { "typeargs", string.Join(",", myClassSymbol.TypeArguments.Select(c=>c.Name)) });
                                File.AppendAllLines("test1.txt", new[] { "typeargs", string.Join(",", myClassSymbol.TypeArguments.Select(c => c.Name)) });
                            }
                            var baseTypeName =  $"{myClassSymbol.BaseType.ContainingNamespace.ToString()}.{myClassSymbol.BaseType.Name}";


                            File.AppendAllLines("test1.txt", new[] { baseType.GetFullName() + " : " + baseTypeName });

                            TypeBuilder entityType = myModule.DefineType(baseType.GetFullName(), TypeAttributes.Public
                                                              | TypeAttributes.Class
                                                              | TypeAttributes.AutoClass
                                                              | TypeAttributes.AnsiClass
                                                              | TypeAttributes.Serializable
                                                              | TypeAttributes.BeforeFieldInit, baseTypesDict.GetOrAdd(baseTypeName, (fullname) => CreateTypeForBaseClass(fullname)));


                            foreach (var property in baseType.Members.OfType<PropertyDeclarationSyntax>())
                            {
                                if((property.AttributeLists.DefaultIfEmpty() ?? Enumerable.Empty<AttributeListSyntax>())
                                        .SelectMany(aa => aa?.Attributes.DefaultIfEmpty() ?? Enumerable.Empty<AttributeSyntax>())
                                  .Any(bb => bb.Name?.NormalizeWhitespace()?.ToFullString() == "ForeignKey"))
                                {
                                    continue;
                                }

                                File.AppendAllLines("test1.txt", new[] { property.Identifier.ToString() });
                                CodeGenerator.CreateProperty(entityType, property.Identifier.ToString(), typeof(string));
                            }
                            File.AppendAllLines("test1.txt", new[] { baseType.GetFullName() +" ok" });
                            return entityType.CreateTypeInfo();
                        }

                        foreach (var baseType in withAtt)
                        {
                            File.AppendAllLines("test1.txt", new[] { $"Base type: {baseType.GetFullName()}" });
                            baseTypes.Add( baseTypesDict.GetOrAdd(baseType.GetFullName(), (fullname)=> CreateTypeForBaseClass(fullname)));
                        }


                    }

                     

                    var generator = new CodeGenerator(new CodeGeneratorOptions
                    {
                        myModule = myModule,
                        Namespace = @namespace,
                        migrationName = $"{@namespace}_{json.SelectToken("$.version") ?? "Initial"}",
                        DTOBaseClasses = baseTypes.ToArray(),




                        MigrationBuilderDropTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable)),
                        MigrationBuilderCreateTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)),
                        MigrationBuilderCreateIndex = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateIndex), new Type[] { typeof(string), typeof(string), typeof(string[]), typeof(string), typeof(bool), typeof(string) }) ?? throw new ArgumentNullException("MigrationBuilderCreateIndex"),
                        MigrationBuilderDropIndex = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropIndex)) ?? throw new ArgumentNullException("MigrationBuilderDropIndex"),

                        ColumnsBuilderType = typeof(ColumnsBuilder),
                        CreateTableBuilderType = typeof(CreateTableBuilder<>),
                        CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                        CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),

                        EntityTypeBuilderType = typeof(EntityTypeBuilder),
                        EntityTypeBuilderPropertyMethod = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), new[] { typeof(string) }),
                        EntityTypeBuilderToTable = typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }),
                        EntityTypeBuilderHasKey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), new[] { typeof(string[]) }),
                        EntityTypeBuilderHasAlternateKey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasAlternateKey), new[] { typeof(string[]) }),

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

                        DynamicMigrationType = typeof(DynamicMigration),
                        MigrationAttributeCtor = typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) }),

                        GeneratePoco = GeneratePoco == "true",

                        IsRequiredMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRequired)),
                        ValueGeneratedOnUpdate = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.ValueGeneratedOnAddOrUpdate)),
                        IsRowVersionMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.IsRowVersion)),
                        HasConversionMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasConversion), new Type[] { }),

                        HasPrecisionMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
                            .GetMethod(nameof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder.HasPrecision), new Type[] { typeof(int), typeof(int) }),
                    });


                    var migrationType = generator.CreateDynamicMigration(json);

                    var tables = generator.GetTables(json, myModule);
                    //I think its here we should generate some openapi spec, looping over the entities in our model.
                    //However i would like the augment the DTO types in the code generator with some attributes that controls it,
                    //so we also can generate it at runtime dynamic for custom entites. 
                    //Same approach as the codegenerator, make a class that is "shared" by the projects and runs on top of the generated DTO classes (EACH DTO class is a endpoint after all).
                    //Remember, dont make it perfect the first time, just get it work and we can add features.

                    //)
                    var enums = new HashSet<Type>();
                    foreach (var type in myModule.GetTypes().Where(t => { try { return t.GetCustomAttribute<EntityDTOAttribute>() != null; } catch (Exception) { } return false; }))
                    {

                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "Generating", "Generated for " + type.GetCustomAttribute<EntityAttribute>().LogicalName, "", DiagnosticSeverity.Info, true), null));

                        context.AddSource($"{type.Name}.cs", GenerateSourceCode(type, GeneratePoco == "true"));

                        foreach(var prop in type.GetProperties().Where(p => (Nullable.GetUnderlyingType( p.PropertyType) ?? p.PropertyType).IsEnum))
                        {
                            enums.Add(Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                        }
                     


                    }
                    foreach (var enumtype in enums)
                    {

                        context.AddSource($"{enumtype.Name}.cs", GenerateSourceCode(enumtype, GeneratePoco == "true"));
                    }

                }
                catch (Exception ex)
                {
                    File.AppendAllLines("err.txt", new[] { ex.ToString() });
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
                    inherience = " : " + inherience;
                }

                if(!generatePoco)
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
                    sb.AppendLine($"\tpublic{(false && type.IsAbstract ? " abstract " : " ")}class {type.Name}{inherience}\r\n\t{{");
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
            }catch(Exception ex)
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
                return $"{gen.Name.Substring(0, gen.Name.IndexOf('`'))}<{string.Join(",", propertyType.GenericTypeArguments.Select(t => SerializeType(t, namespaces)))}>";

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
