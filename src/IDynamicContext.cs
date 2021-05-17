using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;

namespace DotNetDevOps.Extensions.EAVFramework
{
    public interface IDynamicContext
    {
        IReadOnlyDictionary<string, Migration> GetMigrations();
    }
}
