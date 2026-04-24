using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaDbaasManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbInstanceFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectionString",
                table: "DbInstances");

            migrationBuilder.DropColumn(
                name: "Cpu",
                table: "DbInstances");

            migrationBuilder.DropColumn(
                name: "Ram",
                table: "DbInstances");

            migrationBuilder.DropColumn(
                name: "Storage",
                table: "DbInstances");

            migrationBuilder.Sql("ALTER TABLE \"DbInstances\" ALTER COLUMN \"AllocatedPort\" TYPE integer USING \"AllocatedPort\"::integer;");

            migrationBuilder.AddColumn<int>(
                name: "CpuCores",
                table: "DbInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RamMb",
                table: "DbInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StorageGb",
                table: "DbInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpuCores",
                table: "DbInstances");

            migrationBuilder.DropColumn(
                name: "RamMb",
                table: "DbInstances");

            migrationBuilder.DropColumn(
                name: "StorageGb",
                table: "DbInstances");

            migrationBuilder.Sql("ALTER TABLE \"DbInstances\" ALTER COLUMN \"AllocatedPort\" TYPE text USING \"AllocatedPort\"::text;");

            migrationBuilder.AddColumn<string>(
                name: "ConnectionString",
                table: "DbInstances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cpu",
                table: "DbInstances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Ram",
                table: "DbInstances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Storage",
                table: "DbInstances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
