using System.Collections.Generic;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    /// <summary>
    /// Configuration object used when checking for required attributes. Attributes which shouldn't be included in the
    /// check, e.g., attributes populated by the database, can be added to the ignored list. 
    /// </summary>
    public class RequiredSettings
    {
        public List<string> IgnoredFields { get; set; }
    }
}