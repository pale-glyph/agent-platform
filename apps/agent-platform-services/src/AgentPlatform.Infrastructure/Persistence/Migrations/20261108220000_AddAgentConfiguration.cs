using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261108220000_AddAgentConfiguration")]
public partial class AddAgentConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "llm_configuration_id",
            table: "agents",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "agent_mcp_tool_grants",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                mcp_server_id = table.Column<Guid>(type: "uuid", nullable: false),
                tool_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_mcp_tool_grants", item => new { item.agent_id, item.mcp_server_id, item.tool_name });
                table.ForeignKey("FK_agent_mcp_tool_grants_agents_agent_id", item => item.agent_id, principalTable: "agents", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_agent_mcp_tool_grants_mcp_servers_mcp_server_id", item => item.mcp_server_id, principalTable: "mcp_servers", principalColumn: "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_agents_llm_configuration_id", table: "agents", column: "llm_configuration_id");
        migrationBuilder.CreateIndex(name: "IX_agent_mcp_tool_grants_mcp_server_id", table: "agent_mcp_tool_grants", column: "mcp_server_id");
        migrationBuilder.AddForeignKey(name: "FK_agents_llm_configurations_llm_configuration_id", table: "agents", column: "llm_configuration_id", principalTable: "llm_configurations", principalColumn: "id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_agents_llm_configurations_llm_configuration_id", table: "agents");
        migrationBuilder.DropTable(name: "agent_mcp_tool_grants");
        migrationBuilder.DropIndex(name: "IX_agents_llm_configuration_id", table: "agents");
        migrationBuilder.DropColumn(name: "llm_configuration_id", table: "agents");
    }
}
