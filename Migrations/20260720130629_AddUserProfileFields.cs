using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.AspNetUsers', 'LastLoginAt') IS NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [LastLoginAt] datetimeoffset NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.AspNetUsers', 'ProfilePicturePath') IS NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [ProfilePicturePath] nvarchar(max) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ActivityLogs] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(max) NULL,
                        [UserName] nvarchar(200) NULL,
                        [Action] nvarchar(100) NOT NULL,
                        [Description] nvarchar(500) NOT NULL,
                        [EntityName] nvarchar(200) NULL,
                        [EntityId] nvarchar(max) NULL,
                        [IpAddress] nvarchar(50) NULL,
                        [Browser] nvarchar(500) NULL,
                        [Timestamp] datetime2 NOT NULL,
                        [Success] bit NOT NULL,
                        [Details] nvarchar(2000) NULL,
                        CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Notifications] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(max) NOT NULL,
                        [Title] nvarchar(200) NOT NULL,
                        [Message] nvarchar(1000) NOT NULL,
                        [LinkUrl] nvarchar(200) NULL,
                        [RelatedEntityType] nvarchar(100) NULL,
                        [RelatedEntityId] int NULL,
                        [IsRead] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [ActivityLogs];
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.Notifications', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [Notifications];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.AspNetUsers', 'LastLoginAt') IS NOT NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [LastLoginAt];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.AspNetUsers', 'ProfilePicturePath') IS NOT NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [ProfilePicturePath];
                END
            ");
        }
    }
}
