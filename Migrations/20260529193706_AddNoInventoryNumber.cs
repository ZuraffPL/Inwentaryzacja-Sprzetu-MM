using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwentaryzacjaSprzetu.Migrations
{
    /// <inheritdoc />
    public partial class AddNoInventoryNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoInventoryNumber",
                table: "Equipment",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoInventoryNumber",
                table: "Equipment");
        }
    }
}
