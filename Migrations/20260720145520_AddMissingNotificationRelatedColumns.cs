using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingNotificationRelatedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Notifications') AND name = 'RelatedEntityType')
                BEGIN
                    ALTER TABLE [Notifications] ADD [RelatedEntityType] nvarchar(100) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Notifications') AND name = 'RelatedEntityId')
                BEGIN
                    ALTER TABLE [Notifications] ADD [RelatedEntityId] int NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Notifications', 'RelatedEntityType') IS NOT NULL
                BEGIN
                    ALTER TABLE [Notifications] DROP COLUMN [RelatedEntityType];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Notifications', 'RelatedEntityId') IS NOT NULL
                BEGIN
                    ALTER TABLE [Notifications] DROP COLUMN [RelatedEntityId];
                END
            ");
        }
    }
}
