using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class reserve_usage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reserve_usage",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fk_space_reserve_id = table.Column<long>(type: "bigint", nullable: true),
                    fk_resource_reserve_id = table.Column<long>(type: "bigint", nullable: true),
                    fk_account_id = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift = table.Column<EShift>(type: "shift_enum", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    check_in_opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    check_in_closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    check_out_opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    check_out_closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    checked_out_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fk_checked_out_by_id = table.Column<string>(type: "text", nullable: true),
                    check_in_email_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    check_out_email_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    check_in_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    check_out_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reserve_usage", x => x.id);
                    table.CheckConstraint("ck_reserve_usage_single_reserve", "(fk_space_reserve_id IS NULL) <> (fk_resource_reserve_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_reserve_usage_AspNetUsers_fk_account_id",
                        column: x => x.fk_account_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reserve_usage_AspNetUsers_fk_checked_out_by_id",
                        column: x => x.fk_checked_out_by_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reserve_usage_resource_reserve_fk_resource_reserve_id",
                        column: x => x.fk_resource_reserve_id,
                        principalTable: "resource_reserve",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reserve_usage_space_reserve_fk_space_reserve_id",
                        column: x => x.fk_space_reserve_id,
                        principalTable: "space_reserve",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reserve_usage_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fk_usage_id = table.Column<long>(type: "bigint", nullable: false),
                    fk_schedule_id = table.Column<long>(type: "bigint", nullable: false),
                    class_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    modality = table.Column<int>(type: "integer", nullable: false),
                    notebooks_in = table.Column<int>(type: "integer", nullable: false),
                    chargers_in = table.Column<int>(type: "integer", nullable: false),
                    notebooks_out = table.Column<int>(type: "integer", nullable: true),
                    chargers_out = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reserve_usage_entry", x => x.id);
                    table.ForeignKey(
                        name: "FK_reserve_usage_entry_reserve_usage_fk_usage_id",
                        column: x => x.fk_usage_id,
                        principalTable: "reserve_usage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reserve_usage_entry_schedule_fk_schedule_id",
                        column: x => x.fk_schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_check_in_token_hash",
                table: "reserve_usage",
                column: "check_in_token_hash");

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_check_out_token_hash",
                table: "reserve_usage",
                column: "check_out_token_hash");

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_date_status",
                table: "reserve_usage",
                columns: new[] { "date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_fk_account_id",
                table: "reserve_usage",
                column: "fk_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_fk_checked_out_by_id",
                table: "reserve_usage",
                column: "fk_checked_out_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_fk_resource_reserve_id_date_shift",
                table: "reserve_usage",
                columns: new[] { "fk_resource_reserve_id", "date", "shift" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_fk_space_reserve_id_date_shift",
                table: "reserve_usage",
                columns: new[] { "fk_space_reserve_id", "date", "shift" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_entry_fk_schedule_id",
                table: "reserve_usage_entry",
                column: "fk_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_reserve_usage_entry_fk_usage_id_fk_schedule_id",
                table: "reserve_usage_entry",
                columns: new[] { "fk_usage_id", "fk_schedule_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reserve_usage_entry");

            migrationBuilder.DropTable(
                name: "reserve_usage");
        }
    }
}
