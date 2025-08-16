using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideShare_Connect_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTransactionSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTransactionSummaries",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RideId = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTransactionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTransactionSummaries", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_UserTransactionSummaries_Driver_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Driver",
                        principalColumn: "DriverId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTransactionSummaries_Rides_RideId",
                        column: x => x.RideId,
                        principalTable: "Rides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTransactionSummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactionSummaries_DriverId",
                table: "UserTransactionSummaries",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactionSummaries_RideId",
                table: "UserTransactionSummaries",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactionSummaries_UserId",
                table: "UserTransactionSummaries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTransactionSummaries");
        }
    }
}
