using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class textclassfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks");

            migrationBuilder.DropIndex(
                name: "IX_ProfileBooks_BookId",
                table: "ProfileBooks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks",
                columns: new[] { "BookId", "ProfileId", "WasUnsubscribed" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBooks_ProfileId",
                table: "ProfileBooks",
                column: "ProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks");

            migrationBuilder.DropIndex(
                name: "IX_ProfileBooks_ProfileId",
                table: "ProfileBooks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks",
                columns: new[] { "ProfileId", "BookId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBooks_BookId",
                table: "ProfileBooks",
                column: "BookId");
        }
    }
}
