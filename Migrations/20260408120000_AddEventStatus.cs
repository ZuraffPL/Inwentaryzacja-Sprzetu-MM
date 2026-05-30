using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwentaryzacjaSprzetu.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Domyślna wartość 1 = EventStatus.Active — istniejące zdarzenia stają się Aktywnymi
            migrationBuilder.AddColumn<int>(
                name: "EventStatus",
                table: "InventoryEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventStatus",
                table: "InventoryEvents");
        }
    }
}
