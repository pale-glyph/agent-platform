using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261108190000_CreateMcpServers")]
public partial class CreateMcpServers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "mcp_servers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                cache_ttl_seconds = table.Column<int>(type: "integer", nullable: false),
                security = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                api_key = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                cached_tool_definitions = table.Column<string>(type: "jsonb", nullable: true),
                cache_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_mcp_servers", item => item.id);
                table.ForeignKey("FK_mcp_servers_projects_project_id", item => item.project_id, principalTable: "projects", principalColumn: "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_mcp_servers_project_id_name", table: "mcp_servers", columns: new[] { "project_id", "name" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("mcp_servers");
}
