using DotNetDevOps.Extensions.EAVFramework;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
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
 
    public class AlterOperationBuilder<T>
    {

    }
    public class AlterColumnOperation
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


        /// <summary>
        ///     Builds an <see cref="AlterColumnOperation" /> to alter an existing column.
        /// </summary>
        /// <typeparam name="T"> The CLR type that the column is mapped to. </typeparam>
        /// <param name="name"> The column name. </param>
        /// <param name="table"> The name of the table that contains the column. </param>
        /// <param name="type"> The store/database type of the column. </param>
        /// <param name="unicode">
        ///     Indicates whether or not the column can contain Unicode data, or <see langword="null" /> if not specified or not applicable.
        /// </param>
        /// <param name="maxLength">
        ///     The maximum length of data that can be stored in the column, or <see langword="null" /> if not specified or not applicable.
        /// </param>
        /// <param name="rowVersion">
        ///     Indicates whether or not the column acts as an automatic concurrency token, such as a rowversion/timestamp column
        ///     in SQL Server.
        /// </param>
        /// <param name="schema"> The schema that contains the table, or <see langword="null" /> if the default schema should be used. </param>
        /// <param name="nullable"> Indicates whether or not the column can store <see langword="null" /> values. </param>
        /// <param name="defaultValue"> The default value for the column. </param>
        /// <param name="defaultValueSql"> The SQL expression to use for the column's default constraint. </param>
        /// <param name="computedColumnSql"> The SQL expression to use to compute the column value. </param>
        /// <param name="oldClrType">
        ///     The CLR type that the column was previously mapped to. Can be <see langword="null" />, in which case previous value is considered
        ///     unknown.
        /// </param>
        /// <param name="oldType">
        ///     The previous store/database type of the column. Can be <see langword="null" />, in which case previous value is considered unknown.
        /// </param>
        /// <param name="oldUnicode">
        ///     Indicates whether or not the column could previously contain Unicode data, or <see langword="null" /> if not specified or not
        ///     applicable.
        /// </param>
        /// <param name="oldMaxLength">
        ///     The previous maximum length of data that can be stored in the column, or <see langword="null" /> if not specified or not applicable.
        /// </param>
        /// <param name="oldRowVersion">
        ///     Indicates whether or not the column previously acted as an automatic concurrency token, such as a rowversion/timestamp column
        ///     in SQL Server. Can be <see langword="null" />, in which case previous value is considered unknown.
        /// </param>
        /// <param name="oldNullable">
        ///     Indicates whether or not the column could previously store <see langword="null" /> values. Can be <see langword="null" />, in which
        ///     case previous value is
        ///     considered unknown.
        /// </param>
        /// <param name="oldDefaultValue">
        ///     The previous default value for the column. Can be <see langword="null" />, in which case previous value is considered unknown.
        /// </param>
        /// <param name="oldDefaultValueSql">
        ///     The previous SQL expression used for the column's default constraint. Can be <see langword="null" />, in which case previous value is
        ///     considered
        ///     unknown.
        /// </param>
        /// <param name="oldComputedColumnSql">
        ///     The previous SQL expression used to compute the column value. Can be <see langword="null" />, in which case previous value is
        ///     considered unknown.
        /// </param>
        /// <param name="fixedLength"> Indicates whether or not the column is constrained to fixed-length data. </param>
        /// <param name="oldFixedLength"> Indicates whether or not the column was previously constrained to fixed-length data. </param>
        /// <param name="comment"> A comment to associate with the column. </param>
        /// <param name="oldComment"> The previous comment to associate with the column. </param>
        /// <param name="collation"> A collation to apply to the column. </param>
        /// <param name="oldCollation"> The previous collation to apply to the column. </param>
        /// <param name="precision">
        ///     The maximum number of digits that is allowed in this column, or <see langword="null" /> if not specified or not applicable.
        /// </param>
        /// <param name="oldPrecision">
        ///     The previous maximum number of digits that is allowed in this column, or <see langword="null" /> if not specified or not applicable.
        /// </param>
        /// <param name="scale">
        ///     The maximum number of decimal places that is allowed in this column, or <see langword="null" /> if not specified or not applicable.
        /// </param>
        /// <param name="oldScale">
        ///     The previous maximum number of decimal places that is allowed in this column, or <see langword="null" /> if not specified or not
        ///     applicable.
        /// </param>
        /// <param name="stored"> Whether the value of the computed column is stored in the database or not. </param>
        /// <param name="oldStored"> Whether the value of the previous computed column was stored in the database or not. </param>
        /// <returns> A builder to allow annotations to be added to the operation. </returns>
        public virtual AlterOperationBuilder<AlterColumnOperation> AlterColumn<T>(
            [NotNull] string name,
            [NotNull] string table,
            [CanBeNull] string type = null,
            bool? unicode = null,
            int? maxLength = null,
            bool rowVersion = false,
            [CanBeNull] string schema = null,
            bool nullable = false,
            [CanBeNull] object defaultValue = null,
            [CanBeNull] string defaultValueSql = null,
            [CanBeNull] string computedColumnSql = null,
            [CanBeNull] Type oldClrType = null,
            [CanBeNull] string oldType = null,
            bool? oldUnicode = null,
            int? oldMaxLength = null,
            bool oldRowVersion = false,
            bool oldNullable = false,
            [CanBeNull] object oldDefaultValue = null,
            [CanBeNull] string oldDefaultValueSql = null,
            [CanBeNull] string oldComputedColumnSql = null,
            bool? fixedLength = null,
            bool? oldFixedLength = null,
            [CanBeNull] string comment = null,
            [CanBeNull] string oldComment = null,
            [CanBeNull] string collation = null,
            [CanBeNull] string oldCollation = null,
            int? precision = null,
            int? oldPrecision = null,
            int? scale = null,
            int? oldScale = null,
            bool? stored = null,
            bool? oldStored = null)
         => throw new NotImplementedException();

        /// <summary>
        ///     Builds an <see cref="DropForeignKeyOperation" /> to drop an existing foreign key constraint.
        /// </summary>
        /// <param name="name"> The name of the foreign key constraint to drop. </param>
        /// <param name="table"> The table that contains the foreign key. </param>
        /// <param name="schema"> The schema that contains the table, or <see langword="null" /> to use the default schema. </param>
        /// <returns> A builder to allow annotations to be added to the operation. </returns>
        public virtual OperationBuilder<DropForeignKeyOperation> DropForeignKey(
            [NotNull] string name,
            [NotNull] string table,
            [CanBeNull] string schema = null) => throw new NotImplementedException();


        public virtual OperationBuilder<DropTableOperation> DropTable([NotNullAttribute] string name, [CanBeNullAttribute] string schema = null) => throw new NotImplementedException();
        public virtual CreateTableBuilder<TColumns> CreateTable<TColumns>([NotNullAttribute] string name, [NotNullAttribute] Func<ColumnsBuilder, TColumns> columns, [CanBeNullAttribute] string schema = null, [CanBeNullAttribute] Action<CreateTableBuilder<TColumns>> constraints = null, [CanBeNullAttribute] string comment = null) => throw new NotImplementedException();
        public virtual OperationBuilder<DropIndexOperation> DropIndex([NotNullAttribute] string name, [CanBeNullAttribute] string table = null, [CanBeNullAttribute] string schema = null) => throw new NotImplementedException();
        public virtual OperationBuilder<CreateIndexOperation> CreateIndex([NotNullAttribute] string name, [NotNullAttribute] string table, [NotNullAttribute] string[] columns, [CanBeNullAttribute] string schema = null, bool unique = false, [CanBeNullAttribute] string filter = null) => throw new NotImplementedException();
        public virtual OperationBuilder<AddColumnOperation> AddColumn<T>([NotNullAttribute] string name, [NotNullAttribute] string table, [NotNullAttribute] string type = null, bool? unicode = null, int? maxLength = null, bool rowVersion = false, [NotNullAttribute] string schema = null, bool nullable = false, [NotNullAttribute] object defaultValue = null, [NotNullAttribute] string defaultValueSql = null, [NotNullAttribute] string computedColumnSql = null, bool? fixedLength = null, [NotNullAttribute] string comment = null, [NotNullAttribute] string collation = null, int? precision = null, int? scale = null, bool? stored = null) => throw new NotImplementedException();
        public virtual OperationBuilder<AddForeignKeyOperation> AddForeignKey(
            [NotNull] string name,
            [NotNull] string table,
            [NotNull] string column,
            [NotNull] string principalTable,
            [CanBeNull] string schema = null,
            [CanBeNull] string principalSchema = null,
            [CanBeNull] string principalColumn = null,
            ReferentialAction onUpdate = ReferentialAction.NoAction,
            ReferentialAction onDelete = ReferentialAction.NoAction)
            => throw new NotImplementedException();

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
