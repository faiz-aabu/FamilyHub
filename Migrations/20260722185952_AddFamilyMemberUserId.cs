using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyMemberUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FamilyMembers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_UserId",
                table: "FamilyMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyMembers_AspNetUsers_UserId",
                table: "FamilyMembers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_AspNetUsers_UserId",
                table: "FamilyMembers");

            migrationBuilder.DropIndex(
                name: "IX_FamilyMembers_UserId",
                table: "FamilyMembers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FamilyMembers");

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "ActivityLogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Success",
                table: "ActivityLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
