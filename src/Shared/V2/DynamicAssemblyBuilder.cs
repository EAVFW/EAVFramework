using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EAVFramework.Shared.V2
{
    public class DynamicAssemblyBuilder : IDynamicTableBuilder
    {
        public ConcurrentDictionary<string, DynamicTableBuilder> Tables { get; }= new ConcurrentDictionary<string, DynamicTableBuilder>();


        private DynamicCodeService dynamicCodeService;
        public ModuleBuilder Module { get; }
        public string Namespace { get; private set; }

        public DynamicAssemblyBuilder(DynamicCodeService dynamicCodeService, ModuleBuilder myModule, string @namespace)
        {
            if (string.IsNullOrWhiteSpace(@namespace))
            {
                throw new ArgumentException($"'{nameof(@namespace)}' cannot be null or whitespace.", nameof(@namespace));
            }

            this.dynamicCodeService = dynamicCodeService ?? throw new ArgumentNullException(nameof(dynamicCodeService));
            this.Module = myModule ?? throw new ArgumentNullException(nameof(myModule));
            this.Namespace = @namespace;
        }



        public DynamicTableBuilder WithTable(string entityKey, string tableSchemaname, string tableLogicalName, string tableCollectionSchemaName, string schema = "dbo", bool isBaseClass = false)
        {
            return Tables.GetOrAdd(tableSchemaname, (_) => new DynamicTableBuilder(dynamicCodeService, Module, this, entityKey, tableSchemaname, tableLogicalName, tableCollectionSchemaName, schema,isBaseClass));
        }

        public IEnumerable<Type> GetTypes() => this.Module.GetTypes();
    }
}
