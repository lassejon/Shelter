using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shelter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueReviewPerUserPerShelter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_ShelterId_ReviewerId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ShelterId_ReviewerId",
                table: "Reviews",
                columns: new[] { "ShelterId", "ReviewerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_ShelterId_ReviewerId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ShelterId_ReviewerId",
                table: "Reviews",
                columns: new[] { "ShelterId", "ReviewerId" });
        }
    }
}
