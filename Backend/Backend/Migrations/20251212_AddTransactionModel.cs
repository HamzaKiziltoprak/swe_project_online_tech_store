using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderID = table.Column<int>(type: "integer", nullable: false),
                    UserID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_Transactions_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "RefundTransactionID",
                table: "OrderReturns",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderID",
                table: "Transactions",
                column: "OrderID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserID",
                table: "Transactions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_RefundTransactionID",
                table: "OrderReturns",
                column: "RefundTransactionID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderReturns_Transactions_RefundTransactionID",
                table: "OrderReturns",
                column: "RefundTransactionID",
                principalTable: "Transactions",
                principalColumn: "TransactionID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderReturns_Transactions_RefundTransactionID",
                table: "OrderReturns");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_OrderReturns_RefundTransactionID",
                table: "OrderReturns");

            migrationBuilder.DropColumn(
                name: "RefundTransactionID",
                table: "OrderReturns");
        }
    }
}
