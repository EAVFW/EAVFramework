using Newtonsoft.Json.Linq;

namespace EAVFW.Extensions.Manifest.SDK
{
    public interface IManifestPathExtracter
    {
        string ExtractPath(JToken token, string part);
    }
}