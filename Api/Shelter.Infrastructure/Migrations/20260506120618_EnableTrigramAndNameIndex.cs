using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shelter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnableTrigramAndNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable Postgres trigram extension. Powers similarity-based name search in SearchShelterHandler.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // GIN trigram index on Shelter.Name. Without this, similarity queries do a sequential scan.
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""ix_shelters_name_trgm""
                  ON ""Shelters""
                  USING gin (""Name"" gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""ix_shelters_name_trgm"";");
            // Leave the pg_trgm extension installed: dropping it can break other consumers and isn't
            // a typical rollback for this migration.
        }
    }
}
