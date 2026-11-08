using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261108200000_CreateWebhooksAndOutbox")]
public partial class CreateWebhooksAndOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "webhook_endpoints",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                endpoint_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                secret = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                events = table.Column<string>(type: "jsonb", nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                consecutive_failures = table.Column<int>(type: "integer", nullable: false),
                circuit_open_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_webhook_endpoints", item => item.id);
                table.ForeignKey("FK_webhook_endpoints_projects_project_id", item => item.project_id, principalTable: "projects", principalColumn: "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "webhook_deliveries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                webhook_id = table.Column<Guid>(type: "uuid", nullable: false),
                webhook_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                endpoint_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                signing_secret = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                attempt_count = table.Column<int>(type: "integer", nullable: false),
                next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                claimed_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_status_code = table.Column<int>(type: "integer", nullable: true),
                last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_webhook_deliveries", item => item.id));

        migrationBuilder.CreateIndex(name: "IX_webhook_endpoints_project_id_name", table: "webhook_endpoints", columns: new[] { "project_id", "name" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_webhook_deliveries_project_id_status_created_at", table: "webhook_deliveries", columns: new[] { "project_id", "status", "created_at" });
        migrationBuilder.CreateIndex(name: "IX_webhook_deliveries_status_next_attempt_at_claimed_until", table: "webhook_deliveries", columns: new[] { "status", "next_attempt_at", "claimed_until" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "webhook_deliveries");
        migrationBuilder.DropTable(name: "webhook_endpoints");
    }
}
