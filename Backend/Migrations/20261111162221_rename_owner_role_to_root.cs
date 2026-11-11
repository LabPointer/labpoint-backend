using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <summary>
    /// Renomeia o cargo "Owner" para "Root" mantendo os usuários. O IdentitySeeder cria as roles do enum ao iniciar,
    /// então "Root" pode já existir (vazia) quando esta migração rodar: nesse caso os usuários passam de "Owner"
    /// para "Root" e a role antiga é apagada.
    /// </summary>
    public partial class rename_owner_role_to_root : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RenameRole("Owner", "Root"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RenameRole("Root", "Owner"));
        }

        private static string RenameRole(string from, string to)
        {
            var fromNormalized = from.ToUpperInvariant();
            var toNormalized = to.ToUpperInvariant();
            return $"""
                INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
                SELECT ur."UserId", target."Id"
                FROM "AspNetUserRoles" ur
                JOIN "AspNetRoles" source ON source."Id" = ur."RoleId" AND source."NormalizedName" = '{fromNormalized}'
                JOIN "AspNetRoles" target ON target."NormalizedName" = '{toNormalized}'
                ON CONFLICT DO NOTHING;

                DELETE FROM "AspNetRoles"
                WHERE "NormalizedName" = '{fromNormalized}'
                  AND EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "NormalizedName" = '{toNormalized}');

                UPDATE "AspNetRoles"
                SET "Name" = '{to}', "NormalizedName" = '{toNormalized}', "ConcurrencyStamp" = gen_random_uuid()::text
                WHERE "NormalizedName" = '{fromNormalized}';
                """;
        }
    }
}
