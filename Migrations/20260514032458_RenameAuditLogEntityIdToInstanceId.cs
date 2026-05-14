using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaDbaasManager.Migrations
{
    /// <inheritdoc />
    public partial class RenameAuditLogEntityIdToInstanceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "AuditLogs",
                newName: "InstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InstanceId",
                table: "AuditLogs",
                newName: "EntityId");
        }
    }
}
