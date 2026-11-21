using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace AgentPlatform.Infrastructure.Persistence.Migrations;
[DbContext(typeof(PlatformDbContext))]
[Migration("20261120140000_UsePostgresConcurrencyTokens")]
public partial class UsePostgresConcurrencyTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("ALTER TABLE IF EXISTS knowledge_bases DROP COLUMN IF EXISTS row_version; ALTER TABLE IF EXISTS knowledge_base_documents DROP COLUMN IF EXISTS row_version;");
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.AddColumn<uint>(name:"row_version",table:"knowledge_bases",nullable:false,defaultValue:0u); migrationBuilder.AddColumn<uint>(name:"row_version",table:"knowledge_base_documents",nullable:false,defaultValue:0u); }
}
