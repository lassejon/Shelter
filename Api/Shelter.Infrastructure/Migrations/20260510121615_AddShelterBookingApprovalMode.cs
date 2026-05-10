using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shelter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShelterBookingApprovalMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue 0 = BookingApprovalMode.Instant. Existing rows inherit the
            // previous auto-confirm behaviour so the introduction of this column is
            // backwards-compatible from a UX perspective. Owners can opt-in to
            // RequiresApproval per shelter via the edit form.
            migrationBuilder.AddColumn<int>(
                name: "BookingApprovalMode",
                table: "Shelters",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingApprovalMode",
                table: "Shelters");
        }
    }
}
