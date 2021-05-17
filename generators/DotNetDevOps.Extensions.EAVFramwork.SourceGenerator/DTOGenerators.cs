using DotNetDevOps.Extensions.EAVFramwork;
using DotNetDevOps.Extensions.EAVFramwork.Shared;
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramwork
{
    public class DynamicEntity
    {

    }
}
namespace Microsoft.EntityFrameworkCore.Migrations.Operations
{
    public class AddColumnOperation
    {

    }
}

namespace Microsoft.EntityFrameworkCore.Migrations.Operations.Builders
{
    public class ColumnsBuilder
    {
        public virtual OperationBuilder<AddColumnOperation> Column<T>([CanBeNullAttribute] string type = null, bool? unicode = null, int? maxLength = null, bool rowVersion = false, [CanBeNullAttribute] string name = null, bool nullable = false, [CanBeNullAttribute] object defaultValue = null, [CanBeNullAttribute] string defaultValueSql = null, [CanBeNullAttribute] string computedColumnSql = null, bool? fixedLength = null, [CanBeNullAttribute] string comment = null, [CanBeNullAttribute] string collation = null, int? precision = null, int? scale = null, bool? stored = null) => throw new NotImplementedException();
    }

    public class OperationBuilder<T>
    {

    }
}
namespace Microsoft.EntityFrameworkCore.Metadata.Builders
{
    public class EntityTypeBuilder
    {
        public void Property(string name) => throw new NotImplementedException();

        public virtual KeyBuilder HasKey([NotNullAttribute] params string[] propertyNames) => throw new NotImplementedException();


    }
    public static class RelationalEntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder ToTable([NotNullAttribute] this EntityTypeBuilder entityTypeBuilder, [CanBeNullAttribute] string name, [CanBeNullAttribute] string schema) => throw new NotImplementedException();

    }
}
namespace System.ComponentModel.DataAnnotations.Schema
{
    public class ForeignKeyAttribute : Attribute
    {
        public ForeignKeyAttribute(string name)
        {

        }
    }

}
namespace Microsoft.EntityFrameworkCore.Migrations
{
    public class NotNullAttribute : Attribute
    {

    }
    public class CanBeNullAttribute : Attribute
    {

    }
    public class DropTableOperation
    {

    }
    public class MigrationBuilder
    {

        public virtual OperationBuilder<DropTableOperation> DropTable([NotNullAttribute] string name, [CanBeNullAttribute] string schema = null) => throw new NotImplementedException();
        public virtual CreateTableBuilder<TColumns> CreateTable<TColumns>([NotNullAttribute] string name, [NotNullAttribute] Func<ColumnsBuilder, TColumns> columns, [CanBeNullAttribute] string schema = null, [CanBeNullAttribute] Action<CreateTableBuilder<TColumns>> constraints = null, [CanBeNullAttribute] string comment = null) => throw new NotImplementedException();
    }

    public enum ReferentialAction
    {

        NoAction = 0,

        Restrict = 1,

        Cascade = 2,

        SetNull = 3,

        SetDefault = 4
    }

    public class CreateTableBuilder<TColumns>
    {
        //   public const string PrimaryKey = "PrimaryKey";
        // public const string ForeignKey = "ForeignKey";


        public virtual OperationBuilder<AddPrimaryKeyOperation> PrimaryKey([NotNullAttribute] string name, [NotNullAttribute] Expression<Func<TColumns, object>> columns) => throw new NotImplementedException();

        public virtual OperationBuilder<AddForeignKeyOperation> ForeignKey([NotNullAttribute] string name, [NotNullAttribute] Expression<Func<TColumns, object>> column, [NotNullAttribute] string principalTable, [NotNullAttribute] string principalColumn, [CanBeNullAttribute] string principalSchema = null, ReferentialAction onUpdate = ReferentialAction.NoAction, ReferentialAction onDelete = ReferentialAction.NoAction) => throw new NotImplementedException();
        public virtual OperationBuilder<AddForeignKeyOperation> ForeignKey([NotNullAttribute] string name, [NotNullAttribute] Expression<Func<TColumns, object>> columns, [NotNullAttribute] string principalTable, [NotNullAttribute] string[] principalColumns, [CanBeNullAttribute] string principalSchema = null, ReferentialAction onUpdate = ReferentialAction.NoAction, ReferentialAction onDelete = ReferentialAction.NoAction) => throw new NotImplementedException();

    }

    public class AddPrimaryKeyOperation { }

    public class AddForeignKeyOperation { }

    public class KeyBuilder { }




    public class DynamicMigration
    {
        public DynamicMigration(JToken model, IDynamicTable[] tables)
        {
        }
    }
    public class MigrationAttribute : Attribute
    {
        public MigrationAttribute(string name)
        {

        }
    }
}
namespace DotNetDevOps.Extensions.EAVFramwork.Generators
{

    public static class ClassDeclarationSyntaxExtensions
    {
        public const string NESTED_CLASS_DELIMITER = "+";
        public const string NAMESPACE_CLASS_DELIMITER = ".";

        public static string GetFullName(this ClassDeclarationSyntax source)
        {
            Contract.Requires(null != source);

            var items = new List<string>();
            var parent = source.Parent;
            while (parent.IsKind(SyntaxKind.ClassDeclaration))
            {
                var parentClass = parent as ClassDeclarationSyntax;
                Contract.Assert(null != parentClass);
                items.Add(parentClass.Identifier.Text);

                parent = parent.Parent;
            }

            var nameSpace = parent as NamespaceDeclarationSyntax;
            Contract.Assert(null != nameSpace);
            var sb = new StringBuilder().Append(nameSpace.Name).Append(NAMESPACE_CLASS_DELIMITER);
            items.Reverse();
            items.ForEach(i => { sb.Append(i).Append(NESTED_CLASS_DELIMITER); });
            sb.Append(source.Identifier.Text);

            var result = sb.ToString();
            return result;
        }
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
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CustomizationPrefix", out var @namespace);

            var compilation = context.Compilation;


            //  context.Compilation.GetSemanticModel(classSyntax.SyntaxTree)
            var manifest = context.AdditionalFiles.FirstOrDefault(f => Path.GetFileName(f.Path) == "manifest.g.json");
            if (manifest != null)
            {
                try
                {
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
                             .OfType<ClassDeclarationSyntax>()
                             .ToArray();


                        var a = string.Join(",", baseClass
                         .SelectMany(c => c.AttributeLists.DefaultIfEmpty()??Enumerable.Empty<AttributeListSyntax>())
                         .SelectMany(aa => aa?.Attributes.DefaultIfEmpty()??Enumerable.Empty<AttributeSyntax>())
                         .Select(b => b.Name?.NormalizeWhitespace()?.ToFullString()??"dummy"));


                        var withAtt =  baseClass.Where(b=> 
                        (b.AttributeLists.DefaultIfEmpty() ?? Enumerable.Empty<AttributeListSyntax>())
                            .SelectMany(aa => aa?.Attributes.DefaultIfEmpty() ?? Enumerable.Empty<AttributeSyntax>())
                      .Any(bb => bb.Name?.NormalizeWhitespace()?.ToFullString() == "BaseEntity"));



                       // File.WriteAllText("test1.txt",   a );
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

                                File.AppendAllLines("test1.txt", new[] { property.Identifier.ToString() });
                                CodeGenerator.CreateProperty(entityType, property.Identifier.ToString(), typeof(string));
                            }
                            File.AppendAllLines("test1.txt", new[] { baseType.GetFullName() +" ok" });
                            return entityType.CreateTypeInfo();
                        }

                        foreach (var baseType in withAtt)
                        {      
                            baseTypes.Add( baseTypesDict.GetOrAdd(baseType.GetFullName(), (fullname)=> CreateTypeForBaseClass(fullname)));
                        }


                    }



                    var generator = new CodeGenerator(new CodeGeneratorOptions
                    {
                        myModule = myModule,
                        Namespace = @namespace,
                        migrationName = $"{@namespace}_Initial",
                        DTOBaseClasses = baseTypes.ToArray(),




                        MigrationBuilderDropTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.DropTable)),
                        MigrationBuilderCreateTable = typeof(MigrationBuilder).GetMethod(nameof(MigrationBuilder.CreateTable)),
                        ColumnsBuilderType = typeof(ColumnsBuilder),
                        CreateTableBuilderType = typeof(CreateTableBuilder<>),
                        CreateTableBuilderPrimaryKeyName = nameof(CreateTableBuilder<object>.PrimaryKey),
                        CreateTableBuilderForeignKeyName = nameof(CreateTableBuilder<object>.ForeignKey),

                        EntityTypeBuilderType = typeof(EntityTypeBuilder),
                        EntityTypeBuilderPropertyMethod = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.Property), new[] { typeof(string) }),
                        EntityTypeBuilderToTable = typeof(RelationalEntityTypeBuilderExtensions).GetMethod(nameof(RelationalEntityTypeBuilderExtensions.ToTable), new[] { typeof(EntityTypeBuilder), typeof(string), typeof(string) }),
                        EntityTypeBuilderHasKey = typeof(EntityTypeBuilder).GetMethod(nameof(EntityTypeBuilder.HasKey), new[] { typeof(string[]) }),

                        ForeignKeyAttributeCtor = typeof(ForeignKeyAttribute).GetConstructor(new Type[] { typeof(string) }),



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
                        MigrationAttributeCtor = typeof(MigrationAttribute).GetConstructor(new Type[] { typeof(string) })

                    });


                    var migrationType = generator.CreateDynamicMigration(json);

                    var tables = json.SelectToken("$.entities").OfType<JProperty>().Select(entity => Activator.CreateInstance(generator.BuildEntityDefinition(myModule, json, entity)) as IDynamicTable).ToArray();




                    foreach (var type in myModule.GetTypes().Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null))
                    {

                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "test2", type.GetCustomAttribute<EntityAttribute>().LogicalName, "", DiagnosticSeverity.Warning, true), null));

                        context.AddSource($"{type.Name}.cs", GenerateSourceCode(type));
                    }

                }
                catch (Exception ex)
                {
                    File.AppendAllLines("err.txt", new[] { ex.ToString() });
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("100", "test2", ex.StackTrace.ToString(), "", DiagnosticSeverity.Warning, true), null));
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("101", "test2", ex.ToString().Replace("\n", " "), "", DiagnosticSeverity.Warning, true), null));

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


            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("102", "test33", @namespace ?? "empty", "", DiagnosticSeverity.Warning, true), null));
            // context.AddSource("test.cs", "public class HelloWorld{}");
            // Console.WriteLine(test);


        }

        private string GenerateSourceCode(Type type)
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

                GenerateAttributes(sb, "\t", namespaces, type.CustomAttributes);
                sb.AppendLine($"\tpublic class {type.Name}{inherience}\r\n\t{{");
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
            GenerateAttributes(sb, indention, namespaces, prop.CustomAttributes);

            sb.AppendLine($"{indention}public {SerializeType(prop.PropertyType, namespaces)} {prop.Name} {{get;set;}}");
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
