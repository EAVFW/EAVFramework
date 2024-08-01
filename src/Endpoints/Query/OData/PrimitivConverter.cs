namespace EAVFramework.Endpoints.Query.OData
{
    internal class PrimitivConverter : IODataConverter
    {
        public ConvertResult Convert(object data)
        {

            return new ConvertResult { Value = data };
        }
    }
}
