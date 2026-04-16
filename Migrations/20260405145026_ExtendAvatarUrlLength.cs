using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
/**
 Migration类是Entity Framework Core用于管理数据库模式更改的类。
每当你对数据模型进行更改（例如添加新属性、修改现有属性等）时，
EF Core会生成一个新的Migration类来描述这些更改。
此类用于扩展Users表中AvatarUrl字段的长度。
 */
namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class ExtendAvatarUrlLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 5, 14, 50, 25, 495, DateTimeKind.Utc).AddTicks(9928));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 5, 8, 13, 13, 599, DateTimeKind.Utc).AddTicks(551));
        }
    }
}
