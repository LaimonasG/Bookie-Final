using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class paymentuserid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentUsers",
                table: "PaymentUsers");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PaymentUsers",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentUsers",
                table: "PaymentUsers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentUsers_ProfileId",
                table: "PaymentUsers",
                column: "ProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentUsers",
                table: "PaymentUsers");

            migrationBuilder.DropIndex(
                name: "IX_PaymentUsers_ProfileId",
                table: "PaymentUsers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PaymentUsers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentUsers",
                table: "PaymentUsers",
                columns: new[] { "ProfileId", "PaymentId" });
        }
    }
}
