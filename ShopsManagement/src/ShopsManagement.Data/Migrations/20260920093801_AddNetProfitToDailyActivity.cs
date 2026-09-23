using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopsManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNetProfitToDailyActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetProfit",
                table: "DailyActivities",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetProfit",
                table: "DailyActivities");
        }
    }
}
