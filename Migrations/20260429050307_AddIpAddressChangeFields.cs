using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwentaryzacjaSprzetu.Migrations
{
    /// <inheritdoc />
    public partial class AddIpAddressChangeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewIpAddress",
                table: "InventoryEvents",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousIpAddress",
                table: "InventoryEvents",
                type: "TEXT",
                maxLength: 45,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewIpAddress",
                table: "InventoryEvents");

            migrationBuilder.DropColumn(
                name: "PreviousIpAddress",
                table: "InventoryEvents");
        }
    }
}
