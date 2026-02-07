using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EAVFW.Extensions.Manifest.SDK
{
    public interface IManifestEnricher
    {
        Task<JsonDocument> LoadJsonDocumentAsync(JToken jsonraw, string customizationprefix, ILogger logger);
    }
}