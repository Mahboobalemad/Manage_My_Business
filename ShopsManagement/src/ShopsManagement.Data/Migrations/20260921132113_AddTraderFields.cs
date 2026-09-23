using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopsManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTraderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "Traders",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Traders",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Traders",
                type: "TEXT",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "Traders",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "Traders");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Traders");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Traders");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "Traders");
        }
    }
}
