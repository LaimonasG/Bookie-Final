using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class paymenttablefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentUser_Payment_PaymentId",
                table: "PaymentUser");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentUser_Profiles_ProfileId",
                table: "PaymentUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentUser",
                table: "PaymentUser");

            migrationBuilder.RenameTable(
                name: "PaymentUser",
                newName: "PaymentUsers");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentUser_PaymentId",
                table: "PaymentUsers",
                newName: "IX_PaymentUsers_PaymentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentUsers",
                table: "PaymentUsers",
                columns: new[] { "ProfileId", "PaymentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentUsers_Payment_PaymentId",
                table: "PaymentUsers",
                column: "PaymentId",
                principalTable: "Payment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentUsers_Profiles_ProfileId",
                table: "PaymentUsers",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentUsers_Payment_PaymentId",
                table: "PaymentUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentUsers_Profiles_ProfileId",
                table: "PaymentUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentUsers",
                table: "PaymentUsers");

            migrationBuilder.RenameTable(
                name: "PaymentUsers",
                newName: "PaymentUser");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentUsers_PaymentId",
                table: "PaymentUser",
                newName: "IX_PaymentUser_PaymentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentUser",
                table: "PaymentUser",
                columns: new[] { "ProfileId", "PaymentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentUser_Payment_PaymentId",
                table: "PaymentUser",
                column: "PaymentId",
                principalTable: "Payment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentUser_Profiles_ProfileId",
                table: "PaymentUser",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
