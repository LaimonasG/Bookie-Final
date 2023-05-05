using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class changedfieldname : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CoverImagePath",
                table: "Texts",
                newName: "CoverImageUrl");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CoverImageUrl",
                table: "Texts",
                newName: "CoverImagePath");
        }
    }
}
