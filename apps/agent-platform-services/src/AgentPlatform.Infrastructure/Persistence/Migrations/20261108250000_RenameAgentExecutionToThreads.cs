using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261108250000_RenameAgentExecutionToThreads")]
public partial class RenameAgentExecutionToThreads : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_agent_execution_entries_agent_instances_instance_id", "agent_execution_entries");
        migrationBuilder.DropForeignKey("FK_agent_instances_agents_agent_id", "agent_instances");
        migrationBuilder.DropForeignKey("FK_agent_instances_projects_project_id", "agent_instances");
        migrationBuilder.DropPrimaryKey("PK_agent_execution_entries", "agent_execution_entries");
        migrationBuilder.DropPrimaryKey("PK_agent_instances", "agent_instances");
        migrationBuilder.RenameTable("agent_instances", newName: "agent_threads");
        migrationBuilder.RenameTable("agent_execution_entries", newName: "agent_thread_entries");
        migrationBuilder.RenameColumn("instance_id", "agent_thread_entries", "thread_id");
        migrationBuilder.RenameIndex(
            name: "IX_agent_instances_project_id_created_at",
            newName: "IX_agent_threads_project_id_created_at");
        migrationBuilder.RenameIndex(
            name: "IX_agent_instances_agent_id",
            newName: "IX_agent_threads_agent_id");
        migrationBuilder.RenameIndex(
            name: "IX_agent_execution_entries_instance_id_sequence_number",
            newName: "IX_agent_thread_entries_thread_id_sequence_number");
        migrationBuilder.AddPrimaryKey("PK_agent_threads", "agent_threads", "id");
        migrationBuilder.AddPrimaryKey("PK_agent_thread_entries", "agent_thread_entries", "id");
        migrationBuilder.AddForeignKey(
            name: "FK_agent_threads_agents_agent_id",
            table: "agent_threads",
            column: "agent_id",
            principalTable: "agents",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey(
            name: "FK_agent_threads_projects_project_id",
            table: "agent_threads",
            column: "project_id",
            principalTable: "projects",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey(
            name: "FK_agent_thread_entries_agent_threads_thread_id",
            table: "agent_thread_entries",
            column: "thread_id",
            principalTable: "agent_threads",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_agent_thread_entries_agent_threads_thread_id", "agent_thread_entries");
        migrationBuilder.DropForeignKey("FK_agent_threads_agents_agent_id", "agent_threads");
        migrationBuilder.DropForeignKey("FK_agent_threads_projects_project_id", "agent_threads");
        migrationBuilder.DropPrimaryKey("PK_agent_thread_entries", "agent_thread_entries");
        migrationBuilder.DropPrimaryKey("PK_agent_threads", "agent_threads");
        migrationBuilder.RenameTable("agent_thread_entries", newName: "agent_execution_entries");
        migrationBuilder.RenameTable("agent_threads", newName: "agent_instances");
        migrationBuilder.RenameColumn("thread_id", "agent_execution_entries", "instance_id");
        migrationBuilder.RenameIndex(
            name: "IX_agent_thread_entries_thread_id_sequence_number",
            newName: "IX_agent_execution_entries_instance_id_sequence_number");
        migrationBuilder.RenameIndex(
            name: "IX_agent_threads_project_id_created_at",
            newName: "IX_agent_instances_project_id_created_at");
        migrationBuilder.RenameIndex(
            name: "IX_agent_threads_agent_id",
            newName: "IX_agent_instances_agent_id");
        migrationBuilder.AddPrimaryKey("PK_agent_execution_entries", "agent_execution_entries", "id");
        migrationBuilder.AddPrimaryKey("PK_agent_instances", "agent_instances", "id");
        migrationBuilder.AddForeignKey(
            name: "FK_agent_instances_agents_agent_id",
            table: "agent_instances",
            column: "agent_id",
            principalTable: "agents",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey(
            name: "FK_agent_instances_projects_project_id",
            table: "agent_instances",
            column: "project_id",
            principalTable: "projects",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey(
            name: "FK_agent_execution_entries_agent_instances_instance_id",
            table: "agent_execution_entries",
            column: "instance_id",
            principalTable: "agent_instances",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}
