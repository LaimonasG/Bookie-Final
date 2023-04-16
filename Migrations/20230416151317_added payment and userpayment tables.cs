using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bakalauras.Migrations
{
    public partial class addedpaymentanduserpaymenttables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_DailyQuestions_DailyQuestionId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_DailyQuestionId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "DailyQuestionId",
                table: "Answers");

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "DailyQuestionProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Correct",
                table: "Answers",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Price = table.Column<double>(type: "float", nullable: false),
                    Points = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentUser",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentUser", x => new { x.ProfileId, x.PaymentId });
                    table.ForeignKey(
                        name: "FK_PaymentUser_Payment_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentUser_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentUser_PaymentId",
                table: "PaymentUser",
                column: "PaymentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentUser");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "DailyQuestionProfiles");

            migrationBuilder.AlterColumn<bool>(
                name: "Correct",
                table: "Answers",
                type: "bit",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DailyQuestionId",
                table: "Answers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Answers_DailyQuestionId",
                table: "Answers",
                column: "DailyQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_DailyQuestions_DailyQuestionId",
                table: "Answers",
                column: "DailyQuestionId",
                principalTable: "DailyQuestions",
                principalColumn: "Id");
        }
    }
}
