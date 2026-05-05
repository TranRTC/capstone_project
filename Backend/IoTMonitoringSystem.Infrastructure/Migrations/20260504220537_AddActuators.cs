using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActuators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActuatorId",
                table: "DeviceCommands",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Actuators",
                columns: table => new
                {
                    ActuatorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnalogMin = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    AnalogMax = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ControlUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FeedbackSensorId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actuators", x => x.ActuatorId);
                    table.ForeignKey(
                        name: "FK_Actuators_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actuators_Sensors_FeedbackSensorId",
                        column: x => x.FeedbackSensorId,
                        principalTable: "Sensors",
                        principalColumn: "SensorId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_ActuatorId",
                table: "DeviceCommands",
                column: "ActuatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_DeviceId_ActuatorId",
                table: "DeviceCommands",
                columns: new[] { "DeviceId", "ActuatorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Actuators_DeviceId_Name",
                table: "Actuators",
                columns: new[] { "DeviceId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Actuators_FeedbackSensorId",
                table: "Actuators",
                column: "FeedbackSensorId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceCommands_Actuators_ActuatorId",
                table: "DeviceCommands",
                column: "ActuatorId",
                principalTable: "Actuators",
                principalColumn: "ActuatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceCommands_Actuators_ActuatorId",
                table: "DeviceCommands");

            migrationBuilder.DropTable(
                name: "Actuators");

            migrationBuilder.DropIndex(
                name: "IX_DeviceCommands_ActuatorId",
                table: "DeviceCommands");

            migrationBuilder.DropIndex(
                name: "IX_DeviceCommands_DeviceId_ActuatorId",
                table: "DeviceCommands");

            migrationBuilder.DropColumn(
                name: "ActuatorId",
                table: "DeviceCommands");
        }
    }
}
