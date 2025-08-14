using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideShare_Connect_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTokenFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "PaymentMethods");

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransactionId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "PaymentMethods",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Last4",
                table: "PaymentMethods",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentToken",
                table: "PaymentMethods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderTransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "Last4",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "PaymentToken",
                table: "PaymentMethods");

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "PaymentMethods",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "PaymentMethods",
                type: "datetime2",
                nullable: true);
        }
    }
}
