using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNest.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCommunityAndProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ForumTopics_ForumId",
                table: "ForumTopics");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "ForumTopics",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTitle",
                table: "ForumTopics",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "Forums",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Forums",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FavouriteQuote",
                table: "AspNetUsers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql("UPDATE Forums SET NormalizedName = UPPER(LTRIM(RTRIM(Name)))");
            migrationBuilder.Sql("UPDATE ForumTopics SET NormalizedTitle = UPPER(LTRIM(RTRIM(Title)))");

            migrationBuilder.CreateIndex(
                name: "IX_ForumTopics_ForumId_NormalizedTitle",
                table: "ForumTopics",
                columns: new[] { "ForumId", "NormalizedTitle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Forums_BookId",
                table: "Forums",
                column: "BookId",
                unique: true,
                filter: "[BookId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Forums_NormalizedName",
                table: "Forums",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Forums_Books_BookId",
                table: "Forums",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Forums_Books_BookId",
                table: "Forums");

            migrationBuilder.DropIndex(
                name: "IX_ForumTopics_ForumId_NormalizedTitle",
                table: "ForumTopics");

            migrationBuilder.DropIndex(
                name: "IX_Forums_BookId",
                table: "Forums");

            migrationBuilder.DropIndex(
                name: "IX_Forums_NormalizedName",
                table: "Forums");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "ForumTopics");

            migrationBuilder.DropColumn(
                name: "NormalizedTitle",
                table: "ForumTopics");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "Forums");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Forums");

            migrationBuilder.DropColumn(
                name: "FavouriteQuote",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_ForumTopics_ForumId",
                table: "ForumTopics",
                column: "ForumId");
        }
    }
}
