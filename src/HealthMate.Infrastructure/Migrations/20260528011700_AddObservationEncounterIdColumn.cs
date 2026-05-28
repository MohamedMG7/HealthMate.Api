using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddObservationEncounterIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EncounterId",
                table: "Observations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Observations_EncounterId",
                table: "Observations",
                column: "EncounterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Observations_Encounters_EncounterId",
                table: "Observations",
                column: "EncounterId",
                principalTable: "Encounters",
                principalColumn: "Encounter_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Observations_Encounters_EncounterId",
                table: "Observations");

            migrationBuilder.DropIndex(
                name: "IX_Observations_EncounterId",
                table: "Observations");

            migrationBuilder.DropColumn(
                name: "EncounterId",
                table: "Observations");
        }
    }
}
