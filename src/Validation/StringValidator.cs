using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    public class StringValidator : IValidator<string>
    {
        public bool ValidationPassed(string input, JToken manifest, out string error)
        {
           // input = input ?? string.Empty;
            // TODO: Add format 
            var minLength = manifest.SelectToken("$.minLength")?.Value<int>();
            // Factor det ud i facotry pattern
            // En validator for hver type.
            // En validator op mod en type
            if (minLength.HasValue && input.Length > minLength)
            {
                error = $"Minimum length is {minLength}";
                return false;
                // context.AddValidationError(x => x, $"Minimum length is {minLength}", n.ToLower());
            }

            var maxLength = manifest.SelectToken("$.maxLength")?.Value<int>();
            if (maxLength.HasValue && input.Length > maxLength)
            {
                error = $"Minimum length is {maxLength}";
                return false;
                // context.AddValidationError(x => x, $"Minimum length is {maxLength}", n.ToLower());
            }

            error = null;
            return true;
        }
    }
}