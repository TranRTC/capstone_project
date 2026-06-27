using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentInsights",
                columns: table => new
                {
                    AgentInsightId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TriggerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DedupeKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SuggestedActionsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RelatedAlertId = table.Column<long>(type: "bigint", nullable: true),
                    RelatedDeviceId = table.Column<int>(type: "int", nullable: true),
                    ToolsUsedJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsedLlm = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentInsights", x => x.AgentInsightId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentInsights_DedupeKey",
                table: "AgentInsights",
                column: "DedupeKey");

            migrationBuilder.CreateIndex(
                name: "IX_AgentInsights_Status_CreatedAt",
                table: "AgentInsights",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentInsights");
        }
    }
}
