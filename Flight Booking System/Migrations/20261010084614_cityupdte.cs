using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flight_Booking_System.Migrations
{
    /// <inheritdoc />
    public partial class cityupdte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cityId",
                table: "AirPorts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AirPorts_cityId",
                table: "AirPorts",
                column: "cityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AirPorts_Cities_cityId",
                table: "AirPorts",
                column: "cityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AirPorts_Cities_cityId",
                table: "AirPorts");

            migrationBuilder.DropIndex(
                name: "IX_AirPorts_cityId",
                table: "AirPorts");

            migrationBuilder.DropColumn(
                name: "cityId",
                table: "AirPorts");
        }
    }
}
