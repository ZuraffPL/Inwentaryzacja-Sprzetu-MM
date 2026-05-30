using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwentaryzacjaSprzetu.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentChangeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewDepartmentName",
                table: "InventoryEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousDepartmentName",
                table: "InventoryEvents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewDepartmentName",
                table: "InventoryEvents");

            migrationBuilder.DropColumn(
                name: "PreviousDepartmentName",
                table: "InventoryEvents");
        }
    }
}
