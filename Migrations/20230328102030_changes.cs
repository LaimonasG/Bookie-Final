using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class changes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentPeriodDays",
                table: "Books");

            migrationBuilder.RenameColumn(
                name: "LastBookPaymentDates",
                table: "Profiles",
                newName: "LastBookChapterPayments");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Books",
                newName: "ChapterPrice");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastBookChapterPayments",
                table: "Profiles",
                newName: "LastBookPaymentDates");

            migrationBuilder.RenameColumn(
                name: "ChapterPrice",
                table: "Books",
                newName: "Price");

            migrationBuilder.AddColumn<int>(
                name: "PaymentPeriodDays",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
