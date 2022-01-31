using DotNetDevOps.Extensions.EAVFramework;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace Microsoft.EntityFrameworkCore.Migrations
{
    public class NotNullAttribute : Attribute
    {

    }
    public class CanBeNullAttribute : Attribute
    {

    }
    public class DropTableOperation
    {

    }
    public class SqlOperation
    {

    }
    public class DropIndexOperation
    {

    }
    public class CreateIndexOperation
    {

        
    }
    public class MigrationBuilder
    {
        // Summary:
        //     Builds an Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation to
        //     execute raw SQL.
        //
        // Parameters:
        //   sql:
        //     The SQL string to be executed to perform the operation.
        //
        //   suppressTransaction:
        //     Indicates whether or not transactions will be suppressed while executing the
        //     SQL.
        //
        // Returns:
        //     A builder to allow annotations to be added to the operation.
        public virtual OperationBuilder<SqlOperation> Sql([NotNullAttribute] string sql, bool suppressTransaction = false) => throw new NotImplementedException();
         

        public virtual OperationBuilder<DropTableOperation> DropTable([NotNullAttribute] string name, [CanBeNullAttribute] string schema = null) => throw new NotImplementedException();
        public virtual CreateTableBuilder<TColumns> CreateTable<TColumns>([NotNullAttribute] string name, [NotNullAttribute] Func<ColumnsBuilder, TColumns> columns, [CanBeNullAttribute] string schema = null, [CanBeNullAttribute] Action<CreateTableBuilder<TColumns>> constraints = null, [CanBeNullAttribute] string comment = null) => throw new NotImplementedException();
        public virtual OperationBuilder<DropIndexOperation> DropIndex([NotNullAttribute] string name, [CanBeNullAttribute] string table = null, [CanBeNullAttribute] string schema = null) => throw new NotImplementedException();
        public virtual OperationBuilder<CreateIndexOperation> CreateIndex([NotNullAttribute] string name, [NotNullAttribute] string table, [NotNullAttribute] string[] columns, [CanBeNullAttribute] string schema = null, bool unique = false, [CanBeNullAttribute] string filter = null) => throw new NotImplementedException();
    }

    public enum ReferentialAction
    {

        NoAction = 0,

        Restrict = 1,

        Cascade = 2,

        SetNull = 3,

        SetDefault = 4
    }

    public class CreateTableBuilder<TColumns>
    {
        //   public const string PrimaryKey = "PrimaryKey";
        // public const string ForeignKey = "ForeignKey";


        public virtual OperationBuilder<AddPrimaryKeyOperation> PrimaryKey([NotNullAttribute] string name, [NotNullAttribute] Expression<Func<TColumns, object>> columns) => throw new NotImplementedException();

        public virtual OperationBuilder<AddForeignKeyOperation> ForeignKey([NotNullAttribute] string name, [NotNullAttribute] Expression<Func<TColumns, object>> column, [NotNullAttribute] string principalTable, [NotNullAttribute] string principalColumn, [CanBeNullAttribute] string principalSchema = null, ReferentialAction onUpdate = ReferentialAction.NoAction, ReferentialAction onDelete = ReferentialAction.NoAction) => throw new NotImplementedException();
        public virtual OperationBuilder<AddForeignKeyOperation> ForeignKey([NotNullAttribute] string name, [NotNullAttribute] Expression<Func<TColumns, object>> columns, [NotNullAttribute] string principalTable, [NotNullAttribute] string[] principalColumns, [CanBeNullAttribute] string principalSchema = null, ReferentialAction onUpdate = ReferentialAction.NoAction, ReferentialAction onDelete = ReferentialAction.NoAction) => throw new NotImplementedException();

    }

    public class AddPrimaryKeyOperation { }

    public class AddForeignKeyOperation { }

    public class KeyBuilder { }




    
}
