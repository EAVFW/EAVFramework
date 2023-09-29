using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace EAVFramework
{
    public class UtcValueConverter : ValueConverter<DateTime, DateTime>
    {
        public static UtcValueConverter Instance = new UtcValueConverter();
        public UtcValueConverter()
            : base(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}
