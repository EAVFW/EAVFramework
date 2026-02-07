using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFCoreDefaultTPTMigration.Migrations
{
    /// <inheritdoc />
    public partial class AddedKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Identities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Identities_Identities_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Identities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Identities_Identities_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Identities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Identities_Identities_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Identities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecurityGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBusinessUnit = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityGroups_Identities_Id",
                        column: x => x.Id,
                        principalTable: "Identities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    AllowNotifications = table.Column<bool>(type: "bit", nullable: true),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FieldMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstLogon = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLogon = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NemLoginRID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShouldResetPassword = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Identities_Id",
                        column: x => x.Id,
                        principalTable: "Identities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Identities_CreatedById",
                table: "Identities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Identities_ModifiedById",
                table: "Identities",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Identities_OwnerId",
                table: "Identities",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityGroups");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Identities");
        }
    }
}
