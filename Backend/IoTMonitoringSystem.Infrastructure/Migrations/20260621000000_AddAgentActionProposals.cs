using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentActionProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentActionProposals",
                columns: table => new
                {
                    AgentActionProposalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RelatedDeviceId = table.Column<int>(type: "int", nullable: true),
                    RelatedAlertId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentActionProposals", x => x.AgentActionProposalId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentActionProposals_Status_ExpiresAt",
                table: "AgentActionProposals",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentActionProposals_Username_Status",
                table: "AgentActionProposals",
                columns: new[] { "Username", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentActionProposals");
        }
    }
}
