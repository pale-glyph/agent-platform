using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261108230000_AddAgentExecution")]
public partial class AddAgentExecution : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "agent_instances",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                next_sequence_number = table.Column<long>(type: "bigint", nullable: false),
                pending_queued_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                cancellation_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_instances", item => item.id);
                table.ForeignKey("FK_agent_instances_agents_agent_id", item => item.agent_id, principalTable: "agents", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_agent_instances_projects_project_id", item => item.project_id, principalTable: "projects", principalColumn: "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "agent_execution_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                sequence_number = table.Column<long>(type: "bigint", nullable: false),
                type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                delivery = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                message_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                content = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: true),
                tool_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                tool_arguments = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: true),
                tool_result = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_execution_entries", item => item.id);
                table.ForeignKey("FK_agent_execution_entries_agent_instances_instance_id", item => item.instance_id, principalTable: "agent_instances", principalColumn: "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_agent_instances_project_id_created_at", table: "agent_instances", columns: new[] { "project_id", "created_at" });
        migrationBuilder.CreateIndex(name: "IX_agent_instances_agent_id", table: "agent_instances", column: "agent_id");
        migrationBuilder.CreateIndex(name: "IX_agent_execution_entries_instance_id_sequence_number", table: "agent_execution_entries", columns: new[] { "instance_id", "sequence_number" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "agent_execution_entries");
        migrationBuilder.DropTable(name: "agent_instances");
    }
}
