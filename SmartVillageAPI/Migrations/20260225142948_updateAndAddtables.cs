using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartVillageAPI.Migrations
{
    /// <inheritdoc />
    public partial class updateAndAddtables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Devices",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Devices",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGasAutoProtectionEnabled",
                table: "AutomationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "WalletBalance",
                table: "AspNetUsers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "ParkingReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlateNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingReservations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParkingReservations_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WasteCollectionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteCollectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WasteCollectionRequests_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Icon", "IsPublic", "Name" },
                values: new object[] { "any", "leaf", false, "Agriculture" });

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Icon" },
                values: new object[] { "any", "local_parking" });

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "Icon", "Name" },
                values: new object[] { "any", "bolt", "Energy" });

            migrationBuilder.InsertData(
                table: "Zones",
                columns: new[] { "Id", "Description", "Icon", "IsPublic", "Name", "UserId" },
                values: new object[,]
                {
                    { 1, "any", "home", false, "Smart Home", null },
                    { 5, "any", "videocam", true, "Surveillance", null },
                    { 6, "any", "delete", true, "Waste Mgmt", null },
                    { 7, "any", "umbrella", true, "Umbrella", null },
                    { 8, "any", "warning", true, "Emergency", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingReservations_DeviceId",
                table: "ParkingReservations",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingReservations_UserId",
                table: "ParkingReservations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollectionRequests_DeviceId",
                table: "WasteCollectionRequests",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingReservations");

            migrationBuilder.DropTable(
                name: "WasteCollectionRequests");

            migrationBuilder.DeleteData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IsGasAutoProtectionEnabled",
                table: "AutomationSettings");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Icon", "IsPublic", "Name" },
                values: new object[] { "Smart farming and irrigation control", "leaf_icon", true, "Smart Farming" });

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Icon" },
                values: new object[] { "Manage parking spots", "parking_icon" });

            migrationBuilder.UpdateData(
                table: "Zones",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "Icon", "Name" },
                values: new object[] { "Monitor waste bins level", "trash_icon", "Waste Management" });
        }
    }
}
