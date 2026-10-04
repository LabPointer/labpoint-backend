using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:reserve_status_enum", "blocked,booking,canceled,confirmed,rejected")
                .Annotation("Npgsql:Enum:shift_enum", "afternoon,morning,night")
                .OldAnnotation("Npgsql:Enum:reserve_status_enum", "booking,canceled,confirmed,rejected")
                .OldAnnotation("Npgsql:Enum:shift_enum", "afternoon,morning,night");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:reserve_status_enum", "booking,canceled,confirmed,rejected")
                .Annotation("Npgsql:Enum:shift_enum", "afternoon,morning,night")
                .OldAnnotation("Npgsql:Enum:reserve_status_enum", "blocked,booking,canceled,confirmed,rejected")
                .OldAnnotation("Npgsql:Enum:shift_enum", "afternoon,morning,night");
        }
    }
}
