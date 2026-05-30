using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwentaryzacjaSprzetu.Migrations
{
    /// <inheritdoc />
    public partial class AddIpAddressAndLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Department",
                table: "Equipment",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Equipment",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationDetails",
                table: "Equipment",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "LocationDetails",
                table: "Equipment");
        }
    }
}
