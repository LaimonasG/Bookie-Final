using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class commentschange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Comments",
                newName: "EntityType");

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Comments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Comments");

            migrationBuilder.RenameColumn(
                name: "EntityType",
                table: "Comments",
                newName: "Type");
        }
    }
}
