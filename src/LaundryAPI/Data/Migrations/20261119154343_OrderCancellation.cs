using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrderCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledBy",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CancelledBy",
                table: "Orders",
                column: "CancelledBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Accounts_CancelledBy",
                table: "Orders",
                column: "CancelledBy",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Accounts_CancelledBy",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CancelledBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "Orders");
        }
    }
}
