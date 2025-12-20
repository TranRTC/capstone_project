using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FacilityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EdgeDeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EdgeDeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceConfigurations",
                columns: table => new
                {
                    ConfigurationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ConfigurationKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfigurationValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceConfigurations", x => x.ConfigurationId);
                    table.ForeignKey(
                        name: "FK_DeviceConfigurations_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceStatusHistories",
                columns: table => new
                {
                    StatusHistoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceStatusHistories", x => x.StatusHistoryId);
                    table.ForeignKey(
                        name: "FK_DeviceStatusHistories_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationalMetrics",
                columns: table => new
                {
                    MetricId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    MetricType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MetricValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CalculationPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalMetrics", x => x.MetricId);
                    table.ForeignKey(
                        name: "FK_OperationalMetrics_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sensors",
                columns: table => new
                {
                    SensorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    EdgeDeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SensorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SensorType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensors", x => x.SensorId);
                    table.ForeignKey(
                        name: "FK_Sensors_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    AlertRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    SensorId = table.Column<int>(type: "int", nullable: true),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ComparisonOperator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.AlertRuleId);
                    table.ForeignKey(
                        name: "FK_AlertRules_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId");
                    table.ForeignKey(
                        name: "FK_AlertRules_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "SensorId");
                });

            migrationBuilder.CreateTable(
                name: "SensorReadings",
                columns: table => new
                {
                    ReadingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    SensorId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Quality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReadings", x => x.ReadingId);
                    table.ForeignKey(
                        name: "FK_SensorReadings_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SensorReadings_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "SensorId");
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    AlertId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertRuleId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    SensorId = table.Column<int>(type: "int", nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TriggerValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.AlertId);
                    table.ForeignKey(
                        name: "FK_Alerts_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "AlertRuleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "SensorId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_DeviceId",
                table: "AlertRules",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_SensorId",
                table: "AlertRules",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertRuleId",
                table: "Alerts",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DeviceId",
                table: "Alerts",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_SensorId",
                table: "Alerts",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConfigurations_DeviceId",
                table: "DeviceConfigurations",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_EdgeDeviceId",
                table: "Devices",
                column: "EdgeDeviceId",
                unique: true,
                filter: "[EdgeDeviceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceStatusHistories_DeviceId_Timestamp",
                table: "DeviceStatusHistories",
                columns: new[] { "DeviceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalMetrics_DeviceId",
                table: "OperationalMetrics",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_DeviceId_SensorId_Timestamp",
                table: "SensorReadings",
                columns: new[] { "DeviceId", "SensorId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorId",
                table: "SensorReadings",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_Timestamp_DeviceId",
                table: "SensorReadings",
                columns: new[] { "Timestamp", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_DeviceId",
                table: "Sensors",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "DeviceConfigurations");

            migrationBuilder.DropTable(
                name: "DeviceStatusHistories");

            migrationBuilder.DropTable(
                name: "OperationalMetrics");

            migrationBuilder.DropTable(
                name: "SensorReadings");

            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "Sensors");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
