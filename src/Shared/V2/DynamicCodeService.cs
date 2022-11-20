using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.Shared.V2
{
    public class DynamicCodeService
    {
        private ConcurrentDictionary<string, DynamicAssemblyBuilder> Assemblies { get; } = new ConcurrentDictionary<string, DynamicAssemblyBuilder>();
        public CodeGenerationOptions Options { get; }
        public IEmitPropertyService EmitPropertyService { get; }
        public ILookupBuilder LookupPropertyBuilder { get; }
        public IChoiceEnumBuilder ChoiceEnumBuilder { get; }
        public IManifestTypeMapper TypeMapper { get; }

        public IEnumerable<Type> GetTypes() => Assemblies.Values.SelectMany(v => v.GetTypes());

        public DynamicCodeService(CodeGenerationOptions options, 
            IEmitPropertyService emitPropertyService = null,
             ILookupBuilder lookupPropertyBuilder = null,
             IChoiceEnumBuilder choiceEnumBuilder = null,
        IManifestTypeMapper typeMapper = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            EmitPropertyService = emitPropertyService ?? new DefaultEmitPropertyService(options);
            LookupPropertyBuilder = lookupPropertyBuilder ?? new DefaultLookupBuilder(options.MigrationBuilderCreateIndex);
            ChoiceEnumBuilder = choiceEnumBuilder ?? new DefaultChoiceEnumBuilder();
            TypeMapper = typeMapper ?? new DefaultManifestTypeMapper(options);
        }


        public IDictionary<string, string> GenerateCodeFiles()
        {
            var generator = new CodeFileGenerator(Options);
            return Assemblies.Select(c => generator.GenerateSourceCode(c.Value.GetTypes(), false)).SelectMany(c => c).ToDictionary(k => k.Key, k => k.Value);

        }

        public DynamicAssemblyBuilder CreateAssemblyBuilder(string moduleName, string @namespace)
        {
            return Assemblies.GetOrAdd(moduleName, _ =>
            {
                AppDomain myDomain = AppDomain.CurrentDomain;
                AssemblyName myAsmName = new AssemblyName(@namespace);

                var builder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
                  AssemblyBuilderAccess.RunAndCollect);



                ModuleBuilder myModule =
                  builder.DefineDynamicModule(moduleName + ".dll");

                return new DynamicAssemblyBuilder(this, myModule, @namespace);
            });
        }

        internal Type FindParentClasses(string entityKey, string[] allProps)
        {
           return
                Options.DTOBaseClasses.FirstOrDefault(dto => dto.GetCustomAttributes<BaseEntityAttribute>().Any(att => att?.EntityKey == entityKey)) ??
                Options.DTOBaseClasses
                    .Where(dto => dto.GetCustomAttributes<BaseEntityAttribute>(false).Any(attr => string.IsNullOrEmpty(attr.EntityKey)))
                    .Concat(new[] { typeof(DynamicEntity) })
                    .Where(c => CompairProps(c, allProps))
              .OrderByDescending(c => c.GetProperties().Length)
              .First();
        }

        private bool CompairProps(Type c, string[] allProps)
        {
            var allPropsFromType = GetProperties(c).ToArray();
            //  File.AppendAllLines("test1.txt", new[] { $"Compare for {c.Name}: {string.Join(",", allPropsFromType)}|{string.Join(",", allProps)}" });
            return allPropsFromType.All(p => allProps.Contains(p));
        }

        private IEnumerable<string> GetProperties(Type c)
        {
            var fk = c.GetProperties().Where(p => p.GetCustomAttribute(Options.ForeignKeyAttributeCtor.DeclaringType) == null)
                .Select(p => p.Name)
                .ToList();
            return fk;
           
        }

        internal void RemoveNamespace(string @namespace)
        {
            foreach (var k in Assemblies.Keys.Where(k => k.StartsWith($"{@namespace}_")).ToArray())
            {
                while (!Assemblies.TryRemove(k, out var _)) ;
            }
        }
    }
}
