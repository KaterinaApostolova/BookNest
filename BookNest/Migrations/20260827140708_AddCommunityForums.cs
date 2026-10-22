using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNest.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityForums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumPosts_AspNetUsers_UserId",
                table: "ForumPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_ForumTopics_ForumCategories_ForumCategoryId",
                table: "ForumTopics");

            migrationBuilder.RenameTable(
                name: "ForumCategories",
                newName: "Forums");

            migrationBuilder.RenameColumn(
                name: "ForumCategoryId",
                table: "ForumTopics",
                newName: "ForumId");

            migrationBuilder.RenameIndex(
                name: "IX_ForumTopics_ForumCategoryId",
                table: "ForumTopics",
                newName: "IX_ForumTopics_ForumId");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ForumTopics",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ForumPosts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumTopics_UserId",
                table: "ForumTopics",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumPosts_AspNetUsers_UserId",
                table: "ForumPosts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ForumTopics_AspNetUsers_UserId",
                table: "ForumTopics",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ForumTopics_Forums_ForumId",
                table: "ForumTopics",
                column: "ForumId",
                principalTable: "Forums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumPosts_AspNetUsers_UserId",
                table: "ForumPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_ForumTopics_AspNetUsers_UserId",
                table: "ForumTopics");

            migrationBuilder.DropForeignKey(
                name: "FK_ForumTopics_Forums_ForumId",
                table: "ForumTopics");

            migrationBuilder.RenameTable(
                name: "Forums",
                newName: "ForumCategories");

            migrationBuilder.DropIndex(
                name: "IX_ForumTopics_UserId",
                table: "ForumTopics");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ForumTopics");

            migrationBuilder.RenameColumn(
                name: "ForumId",
                table: "ForumTopics",
                newName: "ForumCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_ForumTopics_ForumId",
                table: "ForumTopics",
                newName: "IX_ForumTopics_ForumCategoryId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ForumPosts",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "ForumCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumCategories", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ForumPosts_AspNetUsers_UserId",
                table: "ForumPosts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumTopics_ForumCategories_ForumCategoryId",
                table: "ForumTopics",
                column: "ForumCategoryId",
                principalTable: "ForumCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
