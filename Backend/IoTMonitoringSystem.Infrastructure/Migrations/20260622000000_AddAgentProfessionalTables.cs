using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTMonitoringSystem.Infrastructure.Migrations
{
    public partial class AddAgentProfessionalTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentAuditLogs",
                columns: table => new
                {
                    AgentAuditLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ToolName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RelatedDeviceId = table.Column<int>(type: "int", nullable: true),
                    RelatedAlertId = table.Column<long>(type: "bigint", nullable: true),
                    SessionId = table.Column<long>(type: "bigint", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AgentAuditLogs", x => x.AgentAuditLogId));

            migrationBuilder.CreateTable(
                name: "AgentChatSessions",
                columns: table => new
                {
                    AgentChatSessionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ContextDeviceId = table.Column<int>(type: "int", nullable: true),
                    ContextAlertId = table.Column<long>(type: "bigint", nullable: true),
                    ContextRoute = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AgentChatSessions", x => x.AgentChatSessionId));

            migrationBuilder.CreateTable(
                name: "AgentChatMessages",
                columns: table => new
                {
                    AgentChatMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentChatSessionId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToolsUsedJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataAsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentChatMessages", x => x.AgentChatMessageId);
                    table.ForeignKey(
                        name: "FK_AgentChatMessages_AgentChatSessions_AgentChatSessionId",
                        column: x => x.AgentChatSessionId,
                        principalTable: "AgentChatSessions",
                        principalColumn: "AgentChatSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuditLogs_EventType_CreatedAt",
                table: "AgentAuditLogs",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuditLogs_Username",
                table: "AgentAuditLogs",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_AgentChatMessages_AgentChatSessionId_CreatedAt",
                table: "AgentChatMessages",
                columns: new[] { "AgentChatSessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentChatSessions_Username_UpdatedAt",
                table: "AgentChatSessions",
                columns: new[] { "Username", "UpdatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AgentChatMessages");
            migrationBuilder.DropTable(name: "AgentAuditLogs");
            migrationBuilder.DropTable(name: "AgentChatSessions");
        }
    }
}
