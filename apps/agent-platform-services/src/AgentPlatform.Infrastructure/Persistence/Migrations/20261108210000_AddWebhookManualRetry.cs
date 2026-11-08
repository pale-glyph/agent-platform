using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261108210000_AddWebhookManualRetry")]
public partial class AddWebhookManualRetry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "retry_attempt_count",
            table: "webhook_deliveries",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("UPDATE webhook_deliveries SET retry_attempt_count = attempt_count WHERE status = 'Pending'");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "retry_attempt_count", table: "webhook_deliveries");
}
