using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetDevOps.Extensions.EAVFramework
{
    public interface IDynamicContext
    {
        MigrationsInfo GetMigrations();
    }
    public class MigrationsInfo
    {
        public IReadOnlyDictionary<TypeInfo, Func<Migration>> Factories { get; set; }
        public IReadOnlyDictionary<string, TypeInfo> Types { get; set; }
    }
}
