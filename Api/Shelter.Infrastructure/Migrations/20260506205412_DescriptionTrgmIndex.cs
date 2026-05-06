using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shelter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DescriptionTrgmIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GIN trigram index on Shelter.Description. Backs the word_similarity()-driven
            // description search in SearchShelterHandler. Without this, queries that match against
            // description do a sequential scan over every active shelter.
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""ix_shelters_description_trgm""
                  ON ""Shelters""
                  USING gin (""Description"" gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""ix_shelters_description_trgm"";");
        }
    }
}
