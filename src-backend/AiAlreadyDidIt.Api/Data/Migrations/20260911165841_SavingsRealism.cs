using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAlreadyDidIt.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SavingsRealism : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "est_claimed_tokens",
                table: "apps",
                type: "bigint",
                nullable: true);

            // Data fix-up with the new defaults (12 tokens/line × 3 iterations, cap 5×, $6 / M tokens):
            // keep what uploaders claimed, cap the value the counter uses, derive every cost from the (capped) tokens,
            // and move the two coefficients off their old defaults only where an admin has not changed them.
            migrationBuilder.Sql("UPDATE apps SET est_claimed_tokens = est_generation_tokens WHERE est_is_override AND est_claimed_tokens IS NULL;");
            migrationBuilder.Sql("UPDATE apps SET est_generation_tokens = LEAST(est_generation_tokens, ROUND(GREATEST(source_line_count, 200) * 12 * 3 * 5)::bigint) WHERE est_is_override;");
            migrationBuilder.Sql("UPDATE site_settings SET value = '6' WHERE key = 'savings.price_per_million_tokens' AND value = '15';");
            migrationBuilder.Sql("UPDATE site_settings SET value = '0.3' WHERE key = 'savings.kwh_per_million_tokens' AND value = '0.4';");
            migrationBuilder.Sql("UPDATE apps SET est_generation_cost_usd = ROUND(est_generation_tokens / 1000000.0 * (SELECT value::numeric FROM site_settings WHERE key = 'savings.price_per_million_tokens'), 4);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "est_claimed_tokens",
                table: "apps");
        }
    }
}
