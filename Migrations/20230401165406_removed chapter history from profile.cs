using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class removedchapterhistoryfromprofile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks");

            migrationBuilder.DropIndex(
                name: "IX_ProfileBooks_ProfileId",
                table: "ProfileBooks");

            migrationBuilder.DropColumn(
                name: "LastBookChapterPayments",
                table: "Profiles");

            migrationBuilder.AddColumn<string>(
                name: "BoughtChapterList",
                table: "ProfileBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasUnsubscribed",
                table: "ProfileBooks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks",
                columns: new[] { "ProfileId", "BookId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBooks_BookId",
                table: "ProfileBooks",
                column: "BookId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks");

            migrationBuilder.DropIndex(
                name: "IX_ProfileBooks_BookId",
                table: "ProfileBooks");

            migrationBuilder.DropColumn(
                name: "BoughtChapterList",
                table: "ProfileBooks");

            migrationBuilder.DropColumn(
                name: "WasUnsubscribed",
                table: "ProfileBooks");

            migrationBuilder.AddColumn<string>(
                name: "LastBookChapterPayments",
                table: "Profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileBooks",
                table: "ProfileBooks",
                columns: new[] { "BookId", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBooks_ProfileId",
                table: "ProfileBooks",
                column: "ProfileId");
        }
    }
}
