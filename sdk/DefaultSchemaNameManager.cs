namespace EAVFW.Extensions.Manifest.SDK
{
    public class DefaultSchemaNameManager : ISchemaNameManager
    {
        public string ToSchemaName(string displayName)
        {
            return displayName?.Replace(" ", "").Replace(":", "_").Replace("/", "or").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".",""); ;
        }
    }
}