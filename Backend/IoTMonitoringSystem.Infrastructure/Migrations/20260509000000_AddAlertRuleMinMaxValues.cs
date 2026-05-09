using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertRuleMinMaxValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinValue",
                table: "AlertRules",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                table: "AlertRules",
                type: "decimal(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "AlertRules");
        }
    }
}
