using Newtonsoft.Json.Linq;
using System.Linq;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class DefaultManifestPathExtracter : IManifestPathExtracter
    {
        public string ExtractPath(JToken token, string part)
        {
            string partPath;
            if (token.Path.Contains(part) && !token.Path.EndsWith(part))
            {

                var idx = token.Path.IndexOf(part) + part.Length + 1;

                partPath = new string(token.Path.TakeWhile((c, i) => i < idx || !(c == '.' || c == ']')).ToArray());

                if (partPath.EndsWith('\''))
                    partPath += ']';

            }
            else
            {
                partPath = string.Empty;

            }
            if (partPath.EndsWith("[merge()]"))
            {
                partPath = partPath.Replace("[merge()]", "");
            }
            return string.IsNullOrEmpty(partPath) ? null : partPath;
        }
    }
}