using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shelter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShelterLocationGistIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Shelters_Location",
                table: "Shelters",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shelters_Location",
                table: "Shelters");
        }
    }
}
