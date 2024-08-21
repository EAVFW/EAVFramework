using System;
using System.Text.RegularExpressions;

namespace EAVFramework.Endpoints
{
    public class DataUrlHelper
    {
        public string Name { get; set; }

        public string ContentType { get; set; }

        public byte[] Data { get; set; }

        public void Parse(string dataUrl)
        {
            if (!dataUrl.StartsWith("data:"))
            {
                Data = Convert.FromBase64String(dataUrl);
                return;
            }


            var matches = Regex.Match(dataUrl, @"data:(?<type>.+?);name=(?<name>.+?);base64,(?<data>.+)");

            if (matches.Groups.Count < 3)
            {
                throw new Exception("Invalid DataUrl format");
            }

            ContentType = matches.Groups["type"].Value;
            Name= matches.Groups["name"].Value; ;
            Data =  Convert.FromBase64String(matches.Groups["data"].Value);
        }
    }
}
