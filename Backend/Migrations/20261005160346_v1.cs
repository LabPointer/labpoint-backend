using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resource_reserve",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    purpose = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fk_resource_id = table.Column<long>(type: "bigint", nullable: false),
                    fk_space_reserve_id = table.Column<long>(type: "bigint", nullable: false),
                    fk_account_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_reserve", x => x.id);
                    table.ForeignKey(
                        name: "FK_resource_reserve_AspNetUsers_fk_account_id",
                        column: x => x.fk_account_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource_reserve_resource_fk_resource_id",
                        column: x => x.fk_resource_id,
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource_reserve_space_reserve_fk_space_reserve_id",
                        column: x => x.fk_space_reserve_id,
                        principalTable: "space_reserve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_fk_account_id",
                table: "resource_reserve",
                column: "fk_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_fk_resource_id",
                table: "resource_reserve",
                column: "fk_resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_fk_space_reserve_id",
                table: "resource_reserve",
                column: "fk_space_reserve_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resource_reserve");
        }
    }
}
