using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class textfieldchange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenreId",
                table: "Texts");

            migrationBuilder.AddColumn<string>(
                name: "GenreName",
                table: "Texts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenreName",
                table: "Texts");

            migrationBuilder.AddColumn<int>(
                name: "GenreId",
                table: "Texts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
