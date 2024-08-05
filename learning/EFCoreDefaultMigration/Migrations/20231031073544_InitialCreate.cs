﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFCoreDefaultMigration.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBusinessUnit = table.Column<bool>(type: "bit", nullable: true),
                    Age = table.Column<int>(type: "int", nullable: true),
                    AllowNotifications = table.Column<bool>(type: "bit", nullable: true),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    User_ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FieldMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstLogon = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLogon = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NemLoginRID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShouldResetPassword = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Identity_Identity_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Identity",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Identity_Identity_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Identity",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Identity_Identity_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Identity",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Identity_CreatedById",
                table: "Identity",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Identity_ModifiedById",
                table: "Identity",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Identity_OwnerId",
                table: "Identity",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Identity");
        }
    }
}