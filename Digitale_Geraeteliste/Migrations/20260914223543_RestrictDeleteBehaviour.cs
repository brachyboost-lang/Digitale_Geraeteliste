using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digitale_Geraeteliste.Migrations
{
    /// <inheritdoc />
    public partial class RestrictDeleteBehaviour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_LendItems_Employees_BorrowedById",
                table: "LendItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LendItems_Employees_LendById",
                table: "LendItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LendItems_Items_ItemId",
                table: "LendItems");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LendItems_Employees_BorrowedById",
                table: "LendItems",
                column: "BorrowedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LendItems_Employees_LendById",
                table: "LendItems",
                column: "LendById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LendItems_Items_ItemId",
                table: "LendItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_LendItems_Employees_BorrowedById",
                table: "LendItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LendItems_Employees_LendById",
                table: "LendItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LendItems_Items_ItemId",
                table: "LendItems");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LendItems_Employees_BorrowedById",
                table: "LendItems",
                column: "BorrowedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LendItems_Employees_LendById",
                table: "LendItems",
                column: "LendById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LendItems_Items_ItemId",
                table: "LendItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
