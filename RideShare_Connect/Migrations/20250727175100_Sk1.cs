using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideShare_Connect.Migrations
{
    /// <inheritdoc />
    public partial class Sk1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverRatings_Users_PassengerId",
                table: "DriverRatings");

            migrationBuilder.DropIndex(
                name: "IX_DriverRatings_PassengerId",
                table: "DriverRatings");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DriverRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DriverRatings_UserId",
                table: "DriverRatings",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverRatings_Users_UserId",
                table: "DriverRatings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverRatings_Users_UserId",
                table: "DriverRatings");

            migrationBuilder.DropIndex(
                name: "IX_DriverRatings_UserId",
                table: "DriverRatings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DriverRatings");

            migrationBuilder.CreateIndex(
                name: "IX_DriverRatings_PassengerId",
                table: "DriverRatings",
                column: "PassengerId");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverRatings_Users_PassengerId",
                table: "DriverRatings",
                column: "PassengerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
