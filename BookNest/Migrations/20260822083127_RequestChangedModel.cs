using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNest.Migrations
{
    /// <inheritdoc />
    public partial class RequestChangedModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContributionRequests_AspNetUsers_ReviewedByUserId",
                table: "ContributionRequests");

            migrationBuilder.DropIndex(
                name: "IX_ContributionRequests_ReviewedByUserId",
                table: "ContributionRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ContributionRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ContributionRequests");

            migrationBuilder.RenameColumn(
                name: "AdditionalData",
                table: "ContributionRequests",
                newName: "AdditionalInformation");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ContributionRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "ContributionRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "ContributionRequests");

            migrationBuilder.RenameColumn(
                name: "AdditionalInformation",
                table: "ContributionRequests",
                newName: "AdditionalData");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ContributionRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

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
    }
}
