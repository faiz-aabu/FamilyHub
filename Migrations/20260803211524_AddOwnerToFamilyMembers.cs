using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerToFamilyMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_AspNetUsers_UserId",
                table: "FamilyMembers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "FamilyMembers",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_FamilyMembers_UserId",
                table: "FamilyMembers",
                newName: "IX_FamilyMembers_OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyMembers_AspNetUsers_OwnerId",
                table: "FamilyMembers",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_AspNetUsers_OwnerId",
                table: "FamilyMembers");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "FamilyMembers",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FamilyMembers_OwnerId",
                table: "FamilyMembers",
                newName: "IX_FamilyMembers_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyMembers_AspNetUsers_UserId",
                table: "FamilyMembers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
