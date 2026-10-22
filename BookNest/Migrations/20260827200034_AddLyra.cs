using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNest.Migrations
{
    /// <inheritdoc />
    public partial class AddLyra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BookDiscussionId",
                table: "Conversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "Conversations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_BookId",
                table: "Conversations",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Books_BookId",
                table: "Conversations",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Books_BookId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_BookId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "Conversations");

            migrationBuilder.AlterColumn<int>(
                name: "BookDiscussionId",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
