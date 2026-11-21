using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261121130000_RemoveLegacyKnowledgeBaseTables")]
public sealed class RemoveLegacyKnowledgeBaseTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS agent_knowledge_base_grants
                DROP CONSTRAINT IF EXISTS "FK_agent_knowledge_base_grants_knowledge_bases_knowledge_base_id";
            DROP TABLE IF EXISTS knowledge_base_document_chunks;
            DROP TABLE IF EXISTS knowledge_base_outbox_messages;
            DROP TABLE IF EXISTS knowledge_base_indexing_jobs;
            DROP TABLE IF EXISTS knowledge_base_document_revisions;
            DROP TABLE IF EXISTS knowledge_base_documents;
            DROP TABLE IF EXISTS knowledge_bases;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Legacy knowledge-base data cannot be restored by rolling back this migration.");
}
