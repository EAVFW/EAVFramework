namespace EAVFramework.Shared.V2
{
    public class InverseLookupProp
    {
        public string PropertySchemaName { get; set; }
        public DynamicTableBuilder Table { get; set; }
        public string AttributeKey { get; internal set; }
    }
}
