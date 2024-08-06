namespace EAVFramework.Endpoints.Query.OData
{
    public record ConvertResult
    {
        public object Value { get; set; }
        public long? TotalCount { get; set; }
        public long? PageSize { get;  set; }
        public bool HasMore { get;  set; }
    }
    public interface IODataConverter
    {
        ConvertResult Convert(object data);

    }
}
