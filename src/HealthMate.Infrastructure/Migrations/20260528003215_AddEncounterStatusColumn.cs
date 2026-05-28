using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEncounterStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EncounterStatus",
                table: "Encounters",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncounterStatus",
                table: "Encounters");
        }
    }
}
