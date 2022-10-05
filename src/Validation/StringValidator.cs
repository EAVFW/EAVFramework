using Newtonsoft.Json.Linq;

namespace EAVFramework.Validation
{
    public class StringValidator : IValidator<string>
    {
        public bool ValidationPassed(string input, JToken manifest, out ValidationError error)
        {
            // TODO: Add format 
            var minLength = manifest.SelectToken("$.minLength")?.Value<int>();
            if (minLength.HasValue && input.Length > minLength)
            {
                error = new ValidationError
                {
                    Error = $"Minimum length is {minLength}",
                    Code = "err-minLength",
                    ErrorArgs = new object[] {minLength}
                };
                
                return false;
            }

            var maxLength = manifest.SelectToken("$.maxLength")?.Value<int>();
            if (maxLength.HasValue && input.Length > maxLength)
            {
                error = new ValidationError
                {
                    Error = $"Maximum length is {maxLength}",
                    Code = "err-maxLength",
                    ErrorArgs = new object[] {maxLength}
                };
                
                return false;
            }

            error = null;
            return true;
        }
    }
}