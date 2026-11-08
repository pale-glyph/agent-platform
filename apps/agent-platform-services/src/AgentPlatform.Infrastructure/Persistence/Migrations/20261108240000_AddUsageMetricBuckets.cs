using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261108240000_AddUsageMetricBuckets")]
public partial class AddUsageMetricBuckets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "usage_metric_buckets",
            columns: table => new
            {
                scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                bucket_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                call_count = table.Column<long>(type: "bigint", nullable: false),
                input_tokens = table.Column<long>(type: "bigint", nullable: false),
                cached_input_tokens = table.Column<long>(type: "bigint", nullable: false),
                output_tokens = table.Column<long>(type: "bigint", nullable: false),
                reasoning_tokens = table.Column<long>(type: "bigint", nullable: false),
                total_tokens = table.Column<long>(type: "bigint", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_usage_metric_buckets", x => new { x.scope, x.scope_id, x.bucket_start }));
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "usage_metric_buckets");
}
