using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class resource_reservable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resource_reserve_space_reserve_fk_space_reserve_id",
                table: "resource_reserve");

            migrationBuilder.DropIndex(
                name: "IX_resource_reserve_fk_resource_id",
                table: "resource_reserve");

            migrationBuilder.DropIndex(
                name: "IX_report_from_year_from_month_to_year_to_month",
                table: "report");

            migrationBuilder.AlterColumn<long>(
                name: "fk_space_reserve_id",
                table: "resource_reserve",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<DateOnly>(
                name: "date_from",
                table: "resource_reserve",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "date_to",
                table: "resource_reserve",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "resource_reserve",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<EReserveStatus>(
                name: "status",
                table: "resource_reserve",
                type: "reserve_status_enum",
                nullable: false,
                defaultValue: EReserveStatus.Booking);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "resource",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "resource",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "reservable",
                table: "resource",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "kind",
                table: "report",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "resource_reserve_schedule",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fk_resource_reserve_id = table.Column<long>(type: "bigint", nullable: false),
                    fk_schedule_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_reserve_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_resource_reserve_schedule_resource_reserve_fk_resource_rese~",
                        column: x => x.fk_resource_reserve_id,
                        principalTable: "resource_reserve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource_reserve_schedule_schedule_fk_schedule_id",
                        column: x => x.fk_schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Reservas de recurso existentes eram sempre vinculadas a uma sala e não tinham período próprio:
            // copia datas, status e horários da reserva de sala (passam a ser mantidos em sincronia pelo serviço).
            migrationBuilder.Sql("""
                UPDATE resource_reserve rr
                SET date_from = sr.date_from,
                    date_to = sr.date_to,
                    status = sr.status
                FROM space_reserve sr
                WHERE rr.fk_space_reserve_id = sr.id;

                INSERT INTO resource_reserve_schedule (fk_resource_reserve_id, fk_schedule_id)
                SELECT rr.id, srs.fk_schedule_id
                FROM resource_reserve rr
                JOIN space_reserve_schedule srs ON srs.fk_space_reserve_id = rr.fk_space_reserve_id
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_fk_resource_id_date_from_date_to",
                table: "resource_reserve",
                columns: new[] { "fk_resource_id", "date_from", "date_to" });

            migrationBuilder.CreateIndex(
                name: "IX_resource_name_description",
                table: "resource",
                columns: new[] { "name", "description" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "portuguese");

            migrationBuilder.CreateIndex(
                name: "IX_report_kind_from_year_from_month_to_year_to_month",
                table: "report",
                columns: new[] { "kind", "from_year", "from_month", "to_year", "to_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_schedule_fk_resource_reserve_id_fk_schedul~",
                table: "resource_reserve_schedule",
                columns: new[] { "fk_resource_reserve_id", "fk_schedule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_schedule_fk_schedule_id",
                table: "resource_reserve_schedule",
                column: "fk_schedule_id");

            migrationBuilder.AddForeignKey(
                name: "FK_resource_reserve_space_reserve_fk_space_reserve_id",
                table: "resource_reserve",
                column: "fk_space_reserve_id",
                principalTable: "space_reserve",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resource_reserve_space_reserve_fk_space_reserve_id",
                table: "resource_reserve");

            migrationBuilder.DropTable(
                name: "resource_reserve_schedule");

            migrationBuilder.DropIndex(
                name: "IX_resource_reserve_fk_resource_id_date_from_date_to",
                table: "resource_reserve");

            migrationBuilder.DropIndex(
                name: "IX_resource_name_description",
                table: "resource");

            migrationBuilder.DropIndex(
                name: "IX_report_kind_from_year_from_month_to_year_to_month",
                table: "report");

            migrationBuilder.DropColumn(
                name: "date_from",
                table: "resource_reserve");

            migrationBuilder.DropColumn(
                name: "date_to",
                table: "resource_reserve");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "resource_reserve");

            migrationBuilder.DropColumn(
                name: "status",
                table: "resource_reserve");

            migrationBuilder.DropColumn(
                name: "description",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "reservable",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "report");

            migrationBuilder.AlterColumn<long>(
                name: "fk_space_reserve_id",
                table: "resource_reserve",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_fk_resource_id",
                table: "resource_reserve",
                column: "fk_resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_from_year_from_month_to_year_to_month",
                table: "report",
                columns: new[] { "from_year", "from_month", "to_year", "to_month" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_resource_reserve_space_reserve_fk_space_reserve_id",
                table: "resource_reserve",
                column: "fk_space_reserve_id",
                principalTable: "space_reserve",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
