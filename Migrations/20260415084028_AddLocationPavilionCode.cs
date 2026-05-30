using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwentaryzacjaSprzetu.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationPavilionCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PavilionCode",
                table: "Locations",
                type: "TEXT",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PavilionCode",
                table: "Locations");
        }
    }
}
