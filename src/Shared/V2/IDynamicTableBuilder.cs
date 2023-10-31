namespace EAVFramework.Shared.V2
{
    public interface IDynamicTableBuilder
    {
        DynamicTableBuilder WithTable(string entityKey, string tableSchemaName, string tableLogicalName, string tableCollectionSchemaName, string schema = "dbo", bool isBaseClass=false, MappingStrategy? mappingStrategy = null);
    }
}
