using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableParticipantAndUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Users_UserId1",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Participants_UserId1",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Participants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Participants",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participants_UserId1",
                table: "Participants",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Users_UserId1",
                table: "Participants",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
