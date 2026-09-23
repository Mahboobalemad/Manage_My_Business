using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopsManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddZakatTablesStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ZakatTableId",
                table: "ZakatEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ZakatTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZakatTables", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZakatEntries_ZakatTableId",
                table: "ZakatEntries",
                column: "ZakatTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_ZakatEntries_ZakatTables_ZakatTableId",
                table: "ZakatEntries",
                column: "ZakatTableId",
                principalTable: "ZakatTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZakatEntries_ZakatTables_ZakatTableId",
                table: "ZakatEntries");

            migrationBuilder.DropTable(
                name: "ZakatTables");

            migrationBuilder.DropIndex(
                name: "IX_ZakatEntries_ZakatTableId",
                table: "ZakatEntries");

            migrationBuilder.DropColumn(
                name: "ZakatTableId",
                table: "ZakatEntries");
        }
    }
}
