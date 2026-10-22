using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNest.Migrations
{
    /// <inheritdoc />
    public partial class ChangedContributionRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "ContributionRequests");

            migrationBuilder.DropColumn(
                name: "LinkType",
                table: "ContributionRequests");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "ContributionRequests");

            migrationBuilder.DropColumn(
                name: "WebsiteName",
                table: "ContributionRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ContributionRequests",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "BookId",
                table: "ContributionRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalData",
                table: "ContributionRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "ContributionRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "ContributionRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContributionRequests_ReviewedByUserId",
                table: "ContributionRequests",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionRequests_AspNetUsers_ReviewedByUserId",
                table: "ContributionRequests",
                column: "ReviewedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContributionRequests_AspNetUsers_ReviewedByUserId",
                table: "ContributionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ContributionRequests_ReviewedByUserId",
                table: "ContributionRequests");

            migrationBuilder.DropColumn(
                name: "AdditionalData",
                table: "ContributionRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ContributionRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ContributionRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ContributionRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3000)",
                oldMaxLength: 3000);

            migrationBuilder.AlterColumn<int>(
                name: "BookId",
                table: "ContributionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "ContributionRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkType",
                table: "ContributionRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "ContributionRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteName",
                table: "ContributionRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
