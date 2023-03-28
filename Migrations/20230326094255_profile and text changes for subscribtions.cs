using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class profileandtextchangesforsubscribtions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastBookPaymentDates",
                table: "Profiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePicture",
                table: "Profiles",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextPurchaseDates",
                table: "Profiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PaymentPeriodDays",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "isBlocked",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastBookPaymentDates",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "TextPurchaseDates",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PaymentPeriodDays",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "isBlocked",
                table: "AspNetUsers");
        }
    }
}
