using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActuatorLastKnownState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastKnownState",
                table: "Actuators",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStateAt",
                table: "Actuators",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastKnownState",
                table: "Actuators");

            migrationBuilder.DropColumn(
                name: "LastStateAt",
                table: "Actuators");
        }
    }
}
