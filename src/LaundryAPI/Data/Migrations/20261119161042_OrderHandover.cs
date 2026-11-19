using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrderHandover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderPayments",
                columns: table => new
                {
                    LaundryOrderId = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    AmountVnd = table.Column<decimal>(type: "decimal(29,0)", precision: 29, scale: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPayments", x => x.LaundryOrderId);
                    table.ForeignKey(
                        name: "FK_OrderPayments_Orders_LaundryOrderId",
                        column: x => x.LaundryOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPayments");
        }
    }
}
