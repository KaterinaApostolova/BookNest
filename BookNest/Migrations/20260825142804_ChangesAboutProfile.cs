using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNest.Migrations
{
    /// <inheritdoc />
    public partial class ChangesAboutProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Books_FavouriteBookId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FavouriteBookId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FavouriteBookId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "FavouriteBook",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavouriteBook",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "FavouriteBookId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FavouriteBookId",
                table: "AspNetUsers",
                column: "FavouriteBookId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Books_FavouriteBookId",
                table: "AspNetUsers",
                column: "FavouriteBookId",
                principalTable: "Books",
                principalColumn: "Id");
        }
    }
}
