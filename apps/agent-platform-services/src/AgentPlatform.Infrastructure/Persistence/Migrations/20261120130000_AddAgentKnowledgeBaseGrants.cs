using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AgentPlatform.Infrastructure.Persistence.Migrations;
[DbContext(typeof(PlatformDbContext))]
[Migration("20261120130000_AddAgentKnowledgeBaseGrants")]
public partial class AddAgentKnowledgeBaseGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(name:"agent_knowledge_base_grants",columns:t=>new {agent_id=t.Column<Guid>(nullable:false),knowledge_base_id=t.Column<Guid>(nullable:false)},constraints:t=>{t.PrimaryKey("PK_agent_knowledge_base_grants",x=>new{x.agent_id,x.knowledge_base_id});t.ForeignKey("FK_agent_knowledge_base_grants_agents_agent_id",x=>x.agent_id,"agents","id",onDelete:ReferentialAction.Cascade);});
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("agent_knowledge_base_grants");
}
