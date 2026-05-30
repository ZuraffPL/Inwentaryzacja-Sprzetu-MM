using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwentaryzacjaSprzetu.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedToEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConnectedToEquipmentId",
                table: "Equipment",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_ConnectedToEquipmentId",
                table: "Equipment",
                column: "ConnectedToEquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Equipment_ConnectedToEquipmentId",
                table: "Equipment",
                column: "ConnectedToEquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Equipment_ConnectedToEquipmentId",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_ConnectedToEquipmentId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ConnectedToEquipmentId",
                table: "Equipment");
        }
    }
}
