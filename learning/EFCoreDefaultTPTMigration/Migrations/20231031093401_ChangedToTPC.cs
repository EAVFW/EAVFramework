using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFCoreDefaultTPTMigration.Migrations
{
    /// <inheritdoc />
    public partial class ChangedToTPC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {  
            migrationBuilder.DropForeignKey(
                name: "FK_Identities_Identities_CreatedById",
                table: "Identities");

            migrationBuilder.DropForeignKey(
                name: "FK_Identities_Identities_ModifiedById",
                table: "Identities");

            migrationBuilder.DropForeignKey(
                name: "FK_Identities_Identities_OwnerId",
                table: "Identities");

            migrationBuilder.DropForeignKey(
                name: "FK_SecurityGroups_Identities_Id",
                table: "SecurityGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Identities_Id",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedById",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedOn",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "SecurityGroups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "SecurityGroups",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedById",
                table: "SecurityGroups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedOn",
                table: "SecurityGroups",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SecurityGroups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "SecurityGroups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SecurityGroups",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedById",
                table: "Users",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ModifiedById",
                table: "Users",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OwnerId",
                table: "Users",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGroups_CreatedById",
                table: "SecurityGroups",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGroups_ModifiedById",
                table: "SecurityGroups",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGroups_OwnerId",
                table: "SecurityGroups",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedById",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ModifiedById",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_OwnerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SecurityGroups_CreatedById",
                table: "SecurityGroups");

            migrationBuilder.DropIndex(
                name: "IX_SecurityGroups_ModifiedById",
                table: "SecurityGroups");

            migrationBuilder.DropIndex(
                name: "IX_SecurityGroups_OwnerId",
                table: "SecurityGroups");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModifiedOn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SecurityGroups");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "SecurityGroups");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "SecurityGroups");

            migrationBuilder.DropColumn(
                name: "ModifiedOn",
                table: "SecurityGroups");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SecurityGroups");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "SecurityGroups");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SecurityGroups");

            migrationBuilder.AddForeignKey(
                name: "FK_Identities_Identities_CreatedById",
                table: "Identities",
                column: "CreatedById",
                principalTable: "Identities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Identities_Identities_ModifiedById",
                table: "Identities",
                column: "ModifiedById",
                principalTable: "Identities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Identities_Identities_OwnerId",
                table: "Identities",
                column: "OwnerId",
                principalTable: "Identities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityGroups_Identities_Id",
                table: "SecurityGroups",
                column: "Id",
                principalTable: "Identities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Identities_Id",
                table: "Users",
                column: "Id",
                principalTable: "Identities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
