using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingApplicationUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Notifications') AND name = 'Icon')
                BEGIN
                    ALTER TABLE [Notifications] ADD [Icon] nvarchar(100) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Notifications') AND name = 'Type')
                BEGIN
                    ALTER TABLE [Notifications] ADD [Type] nvarchar(50) NOT NULL CONSTRAINT [DF_Notifications_Type] DEFAULT N'';
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AspNetUsers') AND name = 'DarkModePreference')
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [DarkModePreference] bit NOT NULL CONSTRAINT [DF_AspNetUsers_DarkModePreference] DEFAULT 0;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AspNetUsers') AND name = 'NotificationPreference')
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [NotificationPreference] bit NOT NULL CONSTRAINT [DF_AspNetUsers_NotificationPreference] DEFAULT 0;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AspNetUsers') AND name = 'PreferredLanguage')
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [PreferredLanguage] nvarchar(max) NOT NULL CONSTRAINT [DF_AspNetUsers_PreferredLanguage] DEFAULT N'';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Notifications', 'Icon') IS NOT NULL
                BEGIN
                    ALTER TABLE [Notifications] DROP COLUMN [Icon];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Notifications', 'Type') IS NOT NULL
                BEGIN
                    ALTER TABLE [Notifications] DROP COLUMN [Type];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.AspNetUsers', 'DarkModePreference') IS NOT NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [DarkModePreference];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.AspNetUsers', 'NotificationPreference') IS NOT NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [NotificationPreference];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.AspNetUsers', 'PreferredLanguage') IS NOT NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [PreferredLanguage];
                END
            ");
        }
    }
}
