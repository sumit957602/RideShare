using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideShare_Connect_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleToRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "Rides",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "Rides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Rides_VehicleId",
                table: "Rides",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Vehicles_VehicleId",
                table: "Rides",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Vehicles_VehicleId",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Rides_VehicleId",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "Rides");
        }
    }
}
