using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Shelter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToPostGisLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Enable the PostGIS extension. The Npgsql annotation emits
            //    `CREATE EXTENSION IF NOT EXISTS postgis;` and tracks the dependency in the model.
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            // 2. Add the new column nullable so existing rows can be backfilled before NOT NULL kicks in.
            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Shelters",
                type: "geography (point, 4326)",
                nullable: true);

            // 3. Backfill from the existing double Latitude/Longitude columns.
            //    ST_MakePoint(longitude, latitude) — PostGIS convention is (X, Y) = (lng, lat),
            //    matching NTS' Point(x, y) constructor.
            migrationBuilder.Sql(
                @"UPDATE ""Shelters""
                  SET ""Location"" = ST_SetSRID(ST_MakePoint(""Longitude"", ""Latitude""), 4326)::geography
                  WHERE ""Location"" IS NULL;");

            // 4. Now safe to enforce NOT NULL.
            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Shelters",
                type: "geography (point, 4326)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography (point, 4326)",
                oldNullable: true);

            // 5. Drop the legacy columns.
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Shelters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add the legacy columns nullable, then backfill from Location, then enforce NOT NULL,
            // then drop Location. Mirror of Up for symmetric reversibility.
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Shelters",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Shelters",
                type: "double precision",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""Shelters""
                  SET ""Latitude""  = ST_Y(""Location""::geometry),
                      ""Longitude"" = ST_X(""Location""::geometry)
                  WHERE ""Latitude"" IS NULL;");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Shelters",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Shelters",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Shelters");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
