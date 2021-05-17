using Microsoft.EntityFrameworkCore.Migrations;

namespace DotNetDevOps.Extensions.EAVFramework
{
    public interface IDynamicTable
    {
        //  string Name { get; }
        // T Columns(ColumnsBuilder builder);

        //        void Constraints(CreateTableBuilder<T> builder);

        void Up(MigrationBuilder builder);
        void Down(MigrationBuilder builder);
        //  string Schema { get; }
    }
}
