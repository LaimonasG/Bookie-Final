using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class modifiedprofilebookkey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks",
                columns: new[] { "BookId", "ProfileId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks",
                columns: new[] { "BookId", "ProfileId", "WasUnsubscribed" });
        }
    }
}
