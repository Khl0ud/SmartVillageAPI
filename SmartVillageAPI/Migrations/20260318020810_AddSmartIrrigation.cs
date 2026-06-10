using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartVillageAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartIrrigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IrrigationZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantType = table.Column<int>(type: "int", nullable: false),
                    MoistureThreshold = table.Column<double>(type: "float", nullable: false),
                    CurrentSoilMoisture = table.Column<double>(type: "float", nullable: false),
                    ValveStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAutoMode = table.Column<bool>(type: "bit", nullable: false),
                    LastIrrigatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IrrigationZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IrrigationZones_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IrrigationZones_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IrrigationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IrrigationZoneId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WaterUsedLiters = table.Column<double>(type: "float", nullable: true),
                    SoilMoistureBeforeIrrigation = table.Column<double>(type: "float", nullable: false),
                    SoilMoistureAfterIrrigation = table.Column<double>(type: "float", nullable: true),
                    IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IrrigationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IrrigationLogs_IrrigationZones_IrrigationZoneId",
                        column: x => x.IrrigationZoneId,
                        principalTable: "IrrigationZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IrrigationLogs_IrrigationZoneId",
                table: "IrrigationLogs",
                column: "IrrigationZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_IrrigationZones_UserId",
                table: "IrrigationZones",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IrrigationZones_ZoneId",
                table: "IrrigationZones",
                column: "ZoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IrrigationLogs");

            migrationBuilder.DropTable(
                name: "IrrigationZones");
        }
    }
}
