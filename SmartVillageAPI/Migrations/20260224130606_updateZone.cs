using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartVillageAPI.Migrations
{
    /// <inheritdoc />
    public partial class updateZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Zones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Zones",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsPublic", "UserId" },
                values: new object[] { true, null });

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsPublic", "UserId" },
                values: new object[] { true, null });

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsPublic", "UserId" },
                values: new object[] { true, null });

            migrationBuilder.CreateIndex(
                name: "IX_Zones_UserId",
                table: "Zones",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Zones_AspNetUsers_UserId",
                table: "Zones",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Zones_AspNetUsers_UserId",
                table: "Zones");

            migrationBuilder.DropIndex(
                name: "IX_Zones_UserId",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Zones");

            migrationBuilder.InsertData(
                table: "Zones",
                columns: new[] { "Id", "Description", "Icon", "Name" },
                values: new object[] { 1, "Manage indoor devices and safety", "home_icon", "Smart Home" });
        }
    }
}
