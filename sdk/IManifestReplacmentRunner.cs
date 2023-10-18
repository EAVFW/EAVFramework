using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace EAVFW.Extensions.Manifest.SDK
{
    public interface IManifestReplacmentRunner
    {
        Task RunReplacements(JToken jsonraw, string customizationprefix, ILogger logger, JToken elementToRunReplacementFor = null);
    }
}