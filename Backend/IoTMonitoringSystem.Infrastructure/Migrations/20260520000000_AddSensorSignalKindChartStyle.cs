using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorSignalKindChartStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChartStyle",
                table: "Sensors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignalKind",
                table: "Sensors",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChartStyle",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "SignalKind",
                table: "Sensors");
        }
    }
}
