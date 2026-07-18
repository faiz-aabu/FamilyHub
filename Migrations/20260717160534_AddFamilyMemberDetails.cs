using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyMemberDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "FamilyMembers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "FamilyMembers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "FamilyMembers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "FamilyMembers");
        }
    }
}
