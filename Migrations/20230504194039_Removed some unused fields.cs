using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class Removedsomeunusedfields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "TextPurchaseDates",
                table: "Profiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePicture",
                table: "Profiles",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextPurchaseDates",
                table: "Profiles",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
