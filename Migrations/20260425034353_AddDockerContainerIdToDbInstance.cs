using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaDbaasManager.Migrations
{
    /// <inheritdoc />
    public partial class AddDockerContainerIdToDbInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DockerContainerId",
                table: "DbInstances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DockerContainerId",
                table: "DbInstances");
        }
    }
}
