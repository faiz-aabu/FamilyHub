using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Relationship",
                table: "FamilyMembers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedFamilyMemberId",
                table: "FamilyMembers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_RelatedFamilyMemberId",
                table: "FamilyMembers",
                column: "RelatedFamilyMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyMembers_FamilyMembers_RelatedFamilyMemberId",
                table: "FamilyMembers",
                column: "RelatedFamilyMemberId",
                principalTable: "FamilyMembers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_FamilyMembers_RelatedFamilyMemberId",
                table: "FamilyMembers");

            migrationBuilder.DropIndex(
                name: "IX_FamilyMembers_RelatedFamilyMemberId",
                table: "FamilyMembers");

            migrationBuilder.DropColumn(
                name: "RelatedFamilyMemberId",
                table: "FamilyMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Relationship",
                table: "FamilyMembers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
