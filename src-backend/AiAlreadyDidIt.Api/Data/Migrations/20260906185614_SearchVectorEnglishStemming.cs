using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace AiAlreadyDidIt.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SearchVectorEnglishStemming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "apps",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "setweight(to_tsvector('english', coalesce(name, '')) || to_tsvector('simple', coalesce(name, '')), 'A') || setweight(to_tsvector('english', coalesce(short_description, '')) || to_tsvector('simple', coalesce(short_description, '')), 'B') || setweight(to_tsvector('english', coalesce(tags_text, '') || ' ' || coalesce(category_path_text, '')) || to_tsvector('simple', coalesce(tags_text, '') || ' ' || coalesce(category_path_text, '')), 'C') || setweight(to_tsvector('english', left(coalesce(long_description, ''), 20000)) || to_tsvector('simple', left(coalesce(long_description, ''), 20000)), 'D')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true,
                oldComputedColumnSql: "setweight(to_tsvector('simple', coalesce(name, '')), 'A') || setweight(to_tsvector('simple', coalesce(short_description, '')), 'B') || setweight(to_tsvector('simple', coalesce(tags_text, '') || ' ' || coalesce(category_path_text, '')), 'C') || setweight(to_tsvector('simple', left(coalesce(long_description, ''), 20000)), 'D')",
                oldStored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "apps",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "setweight(to_tsvector('simple', coalesce(name, '')), 'A') || setweight(to_tsvector('simple', coalesce(short_description, '')), 'B') || setweight(to_tsvector('simple', coalesce(tags_text, '') || ' ' || coalesce(category_path_text, '')), 'C') || setweight(to_tsvector('simple', left(coalesce(long_description, ''), 20000)), 'D')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true,
                oldComputedColumnSql: "setweight(to_tsvector('english', coalesce(name, '')) || to_tsvector('simple', coalesce(name, '')), 'A') || setweight(to_tsvector('english', coalesce(short_description, '')) || to_tsvector('simple', coalesce(short_description, '')), 'B') || setweight(to_tsvector('english', coalesce(tags_text, '') || ' ' || coalesce(category_path_text, '')) || to_tsvector('simple', coalesce(tags_text, '') || ' ' || coalesce(category_path_text, '')), 'C') || setweight(to_tsvector('english', left(coalesce(long_description, ''), 20000)) || to_tsvector('simple', left(coalesce(long_description, ''), 20000)), 'D')",
                oldStored: true);
        }
    }
}
