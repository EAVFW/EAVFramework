using System;
using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    public class NumberValidator : IValidator<int>, IValidator<decimal>
    {
        public bool ValidationPassed(int input, JToken manifest, out ValidationError error)
        {
            return CheckIntegerDecimal(input, manifest, out error);
        }
        // TODO: We need to figure out what to do with floating point numbers.
        /*var decimals = m.SelectToken("$.decimals");
        if (decimals != null && i.CompareTo(decimals.Value<T>()) > 0)
        {
            context.AddValidationError(x => x, $"Much be less than {maximum}", n.ToLower());
        }*/

        public bool ValidationPassed(decimal input, JToken manifest, out ValidationError error)
        {
            return CheckIntegerDecimal(input, manifest, out error);
        }

        private bool CheckIntegerDecimal<T>(T input, JToken manifest,
            out ValidationError error) where T : IComparable
        {
            var minimum = manifest.SelectToken("$.minimum");
            if (minimum != null && input.CompareTo(minimum.Value<T>()) < 0)
            {
                error = new ValidationError
                {
                    Error = $"Much be larger than {minimum}",
                    Code = "err-minimum",
                    ErrorArgs = new object[] {minimum},
                   
                };
                return false;
            }

            var maximum = manifest.SelectToken("$.maximum");
            if (maximum != null && input.CompareTo(maximum.Value<T>()) > 0)
            {
                error = new ValidationError
                {
                    Error = $"Much be less than {maximum}",
                    Code = "err-maximum",
                    ErrorArgs = new object[] {maximum}
                };
                return false;
            }

            var exclusiveMinimum = manifest.SelectToken("$.exclusiveMinimum");
            if (exclusiveMinimum != null && input.CompareTo(exclusiveMinimum.Value<T>()) <= 0)
            {
                error = new ValidationError
                {
                    Error = $"Much be less than or equal to {exclusiveMinimum}",
                    Code = "err-exclusiveMinimum",
                    ErrorArgs = new object[] {exclusiveMinimum}
                };
                return false;
            }

            var exclusiveMaximum = manifest.SelectToken("$.exclusiveMaximum");
            if (exclusiveMaximum != null && input.CompareTo(exclusiveMaximum.Value<T>()) >= 0)
            {
                error = new ValidationError
                {
                    Error = $"Much be larger than or equal {exclusiveMaximum}",
                    Code = "err-exclusiveMaximum",
                    ErrorArgs = new object[] {exclusiveMaximum}
                };
                return false;
            }

            error = null;
            return true;
        }
    }
}