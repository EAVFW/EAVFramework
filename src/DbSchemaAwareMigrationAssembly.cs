using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetDevOps.Extensions.EAVFramework
{
   
    public class DbSchemaAwareMigrationAssembly : MigrationsAssembly
    {
        private readonly DbContext _context;
        // private readonly IReadOnlyDictionary<TypeInfo, Func<Migration>> _migrations = new Dictionary<TypeInfo, Func<Migration>>();
        // private readonly IReadOnlyDictionary<string, TypeInfo> _migrationsTypes = new Dictionary<string, TypeInfo>();
        private readonly MigrationsInfo migrations;
        public DbSchemaAwareMigrationAssembly(ICurrentDbContext currentContext,
              IDbContextOptions options, IMigrationsIdGenerator idGenerator,
              IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
          : base(currentContext, options, idGenerator, logger)
        {
            _context = currentContext.Context;

            var dynamicContext = _context as IDynamicContext ?? throw new ArgumentNullException(nameof(_context), "Current Context is not IDynamicContext");


            // var migrations = dynamicContext.GetMigrations();
            // _migrations = migrations.ToDictionary(v => v.Value.GetType().GetTypeInfo(), v => v.Value);
            //            _migrationsTypes = dynamicContext.GetMigrations(); // migrations.ToDictionary(k => k.Key, v => v.Value.GetType().GetTypeInfo());


            //_migrations["Initial_Migration"] = typeof(DynamicMigration).GetTypeInfo();
            migrations = dynamicContext.GetMigrations();

        }

        public override IReadOnlyDictionary<string, TypeInfo> Migrations => migrations.Types;

        public override Migration CreateMigration(TypeInfo migrationClass,
              string activeProvider)
        {
            if (activeProvider == null)
                throw new ArgumentNullException(nameof(activeProvider));


            return migrations.Factories[migrationClass]();


        }
    }
}
