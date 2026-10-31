using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class report_period_range : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_report_year_month",
                table: "report");

            // O relatório gravado é só cache e o formato do JSON mudou (rankings viraram listas),
            // então as linhas antigas são descartadas e recalculadas na próxima consulta.
            migrationBuilder.Sql("DELETE FROM report;");

            migrationBuilder.RenameColumn(
                name: "year",
                table: "report",
                newName: "from_year");

            migrationBuilder.RenameColumn(
                name: "month",
                table: "report",
                newName: "from_month");

            migrationBuilder.AddColumn<int>(
                name: "to_year",
                table: "report",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "to_month",
                table: "report",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_report_from_year_from_month_to_year_to_month",
                table: "report",
                columns: new[] { "from_year", "from_month", "to_year", "to_month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_report_from_year_from_month_to_year_to_month",
                table: "report");

            migrationBuilder.Sql("DELETE FROM report;");

            migrationBuilder.DropColumn(
                name: "to_year",
                table: "report");

            migrationBuilder.DropColumn(
                name: "to_month",
                table: "report");

            migrationBuilder.RenameColumn(
                name: "from_year",
                table: "report",
                newName: "year");

            migrationBuilder.RenameColumn(
                name: "from_month",
                table: "report",
                newName: "month");

            migrationBuilder.CreateIndex(
                name: "IX_report_year_month",
                table: "report",
                columns: new[] { "year", "month" },
                unique: true);
        }
    }
}
