using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class initial_sqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    registration = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<long>(type: "INTEGER", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "holiday",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    source = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "holiday_year_sync",
                columns: table => new
                {
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    synced_at = table.Column<long>(type: "INTEGER", nullable: false),
                    from_api = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday_year_sync", x => x.year);
                });

            migrationBuilder.CreateTable(
                name: "report",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    kind = table.Column<int>(type: "INTEGER", nullable: false),
                    from_year = table.Column<int>(type: "INTEGER", nullable: false),
                    from_month = table.Column<int>(type: "INTEGER", nullable: false),
                    to_year = table.Column<int>(type: "INTEGER", nullable: false),
                    to_month = table.Column<int>(type: "INTEGER", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    generated_at = table.Column<long>(type: "INTEGER", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    reservable = table.Column<bool>(type: "INTEGER", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schedule",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    start_at = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    end_at = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    shift = table.Column<int>(type: "INTEGER", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "space",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    locked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_space", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subject",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "released_date",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    fk_account_id = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<long>(type: "INTEGER", nullable: false),
                    revoked_at = table.Column<long>(type: "INTEGER", nullable: true),
                    keep_existing_reserves = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_released_date", x => x.id);
                    table.ForeignKey(
                        name: "FK_released_date_AspNetUsers_fk_account_id",
                        column: x => x.fk_account_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "space_reserve",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    created_at = table.Column<long>(type: "INTEGER", nullable: false),
                    date_from = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    date_to = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    purpose = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    fk_space_id = table.Column<long>(type: "INTEGER", nullable: false),
                    fk_account_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_space_reserve", x => x.id);
                    table.ForeignKey(
                        name: "FK_space_reserve_AspNetUsers_fk_account_id",
                        column: x => x.fk_account_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_space_reserve_space_fk_space_id",
                        column: x => x.fk_space_id,
                        principalTable: "space",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "space_resource",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_space_id = table.Column<long>(type: "INTEGER", nullable: false),
                    fk_resource_id = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_space_resource", x => x.id);
                    table.ForeignKey(
                        name: "FK_space_resource_resource_fk_resource_id",
                        column: x => x.fk_resource_id,
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_space_resource_space_fk_space_id",
                        column: x => x.fk_space_id,
                        principalTable: "space",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_subject",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_account_id = table.Column<string>(type: "TEXT", nullable: false),
                    fk_subject_id = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_subject", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_subject_AspNetUsers_fk_account_id",
                        column: x => x.fk_account_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_account_subject_subject_fk_subject_id",
                        column: x => x.fk_subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "space_subject",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_space_id = table.Column<long>(type: "INTEGER", nullable: false),
                    fk_subject_id = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_space_subject", x => x.id);
                    table.ForeignKey(
                        name: "FK_space_subject_space_fk_space_id",
                        column: x => x.fk_space_id,
                        principalTable: "space",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_space_subject_subject_fk_subject_id",
                        column: x => x.fk_subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_reserve",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    created_at = table.Column<long>(type: "INTEGER", nullable: false),
                    date_from = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    date_to = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    purpose = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    fk_resource_id = table.Column<long>(type: "INTEGER", nullable: false),
                    fk_space_reserve_id = table.Column<long>(type: "INTEGER", nullable: true),
                    fk_account_id = table.Column<string>(type: "TEXT", nullable: false)
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
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "space_reserve_schedule",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_space_reserve_id = table.Column<long>(type: "INTEGER", nullable: false),
                    fk_schedule_id = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_space_reserve_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_space_reserve_schedule_schedule_fk_schedule_id",
                        column: x => x.fk_schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_space_reserve_schedule_space_reserve_fk_space_reserve_id",
                        column: x => x.fk_space_reserve_id,
                        principalTable: "space_reserve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reserve_usage",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_space_reserve_id = table.Column<long>(type: "INTEGER", nullable: true),
                    fk_resource_reserve_id = table.Column<long>(type: "INTEGER", nullable: true),
                    fk_account_id = table.Column<string>(type: "TEXT", nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    shift = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    check_in_opens_at = table.Column<long>(type: "INTEGER", nullable: false),
                    check_in_closes_at = table.Column<long>(type: "INTEGER", nullable: false),
                    check_out_opens_at = table.Column<long>(type: "INTEGER", nullable: false),
                    check_out_closes_at = table.Column<long>(type: "INTEGER", nullable: false),
                    checked_in_at = table.Column<long>(type: "INTEGER", nullable: true),
                    checked_out_at = table.Column<long>(type: "INTEGER", nullable: true),
                    fk_checked_out_by_id = table.Column<string>(type: "TEXT", nullable: true),
                    check_in_email_sent_at = table.Column<long>(type: "INTEGER", nullable: true),
                    check_out_email_sent_at = table.Column<long>(type: "INTEGER", nullable: true),
                    check_in_token_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    check_out_token_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
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
                name: "resource_reserve_schedule",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_resource_reserve_id = table.Column<long>(type: "INTEGER", nullable: false),
                    fk_schedule_id = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_reserve_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_resource_reserve_schedule_resource_reserve_fk_resource_reserve_id",
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

            migrationBuilder.CreateTable(
                name: "email_outbox",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    to_address = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    subject = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    template = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<long>(type: "INTEGER", nullable: false),
                    next_attempt_at = table.Column<long>(type: "INTEGER", nullable: false),
                    sent_at = table.Column<long>(type: "INTEGER", nullable: true),
                    last_error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    fk_usage_id = table.Column<long>(type: "INTEGER", nullable: true),
                    expires_at = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_outbox", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_outbox_reserve_usage_fk_usage_id",
                        column: x => x.fk_usage_id,
                        principalTable: "reserve_usage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reserve_usage_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_usage_id = table.Column<long>(type: "INTEGER", nullable: false),
                    fk_schedule_id = table.Column<long>(type: "INTEGER", nullable: false),
                    class_name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    modality = table.Column<int>(type: "INTEGER", nullable: false),
                    notebooks_in = table.Column<int>(type: "INTEGER", nullable: false),
                    chargers_in = table.Column<int>(type: "INTEGER", nullable: false),
                    notebooks_out = table.Column<int>(type: "INTEGER", nullable: true),
                    chargers_out = table.Column<int>(type: "INTEGER", nullable: true)
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
                name: "IX_account_subject_fk_account_id_fk_subject_id",
                table: "account_subject",
                columns: new[] { "fk_account_id", "fk_subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_subject_fk_subject_id",
                table: "account_subject",
                column: "fk_subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_registration",
                table: "AspNetUsers",
                column: "registration",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_fk_usage_id",
                table: "email_outbox",
                column: "fk_usage_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_status_next_attempt_at",
                table: "email_outbox",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_holiday_date",
                table: "holiday",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_released_date_date",
                table: "released_date",
                column: "date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_released_date_fk_account_id",
                table: "released_date",
                column: "fk_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_kind_from_year_from_month_to_year_to_month",
                table: "report",
                columns: new[] { "kind", "from_year", "from_month", "to_year", "to_month" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_resource_name",
                table: "resource",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_fk_account_id",
                table: "resource_reserve",
                column: "fk_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_fk_resource_id_date_from_date_to",
                table: "resource_reserve",
                columns: new[] { "fk_resource_id", "date_from", "date_to" });

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_fk_space_reserve_id",
                table: "resource_reserve",
                column: "fk_space_reserve_id");

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_schedule_fk_resource_reserve_id_fk_schedule_id",
                table: "resource_reserve_schedule",
                columns: new[] { "fk_resource_reserve_id", "fk_schedule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_reserve_schedule_fk_schedule_id",
                table: "resource_reserve_schedule",
                column: "fk_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_space_name",
                table: "space",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_space_reserve_fk_account_id",
                table: "space_reserve",
                column: "fk_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_space_reserve_fk_space_id",
                table: "space_reserve",
                column: "fk_space_id");

            migrationBuilder.CreateIndex(
                name: "IX_space_reserve_schedule_fk_schedule_id",
                table: "space_reserve_schedule",
                column: "fk_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_space_reserve_schedule_fk_space_reserve_id_fk_schedule_id",
                table: "space_reserve_schedule",
                columns: new[] { "fk_space_reserve_id", "fk_schedule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_space_resource_fk_resource_id",
                table: "space_resource",
                column: "fk_resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_space_resource_fk_space_id_fk_resource_id",
                table: "space_resource",
                columns: new[] { "fk_space_id", "fk_resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_space_subject_fk_space_id_fk_subject_id",
                table: "space_subject",
                columns: new[] { "fk_space_id", "fk_subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_space_subject_fk_subject_id",
                table: "space_subject",
                column: "fk_subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_name",
                table: "subject",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_subject");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "email_outbox");

            migrationBuilder.DropTable(
                name: "holiday");

            migrationBuilder.DropTable(
                name: "holiday_year_sync");

            migrationBuilder.DropTable(
                name: "released_date");

            migrationBuilder.DropTable(
                name: "report");

            migrationBuilder.DropTable(
                name: "reserve_usage_entry");

            migrationBuilder.DropTable(
                name: "resource_reserve_schedule");

            migrationBuilder.DropTable(
                name: "space_reserve_schedule");

            migrationBuilder.DropTable(
                name: "space_resource");

            migrationBuilder.DropTable(
                name: "space_subject");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "reserve_usage");

            migrationBuilder.DropTable(
                name: "schedule");

            migrationBuilder.DropTable(
                name: "subject");

            migrationBuilder.DropTable(
                name: "resource_reserve");

            migrationBuilder.DropTable(
                name: "resource");

            migrationBuilder.DropTable(
                name: "space_reserve");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "space");
        }
    }
}
