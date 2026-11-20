using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class email_outbox_usage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "email_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "fk_usage_id",
                table: "email_outbox",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_fk_usage_id",
                table: "email_outbox",
                column: "fk_usage_id");

            migrationBuilder.AddForeignKey(
                name: "FK_email_outbox_reserve_usage_fk_usage_id",
                table: "email_outbox",
                column: "fk_usage_id",
                principalTable: "reserve_usage",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_outbox_reserve_usage_fk_usage_id",
                table: "email_outbox");

            migrationBuilder.DropIndex(
                name: "IX_email_outbox_fk_usage_id",
                table: "email_outbox");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "email_outbox");

            migrationBuilder.DropColumn(
                name: "fk_usage_id",
                table: "email_outbox");
        }
    }
}
