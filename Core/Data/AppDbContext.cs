using Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Models.Account;
using Models.Calendar;
using Models.Email;
using Models.Report;
using Models.Reserve;
using Models.Resource;
using Models.Schedule;
using Models.Subject;
using Models.Usage;

namespace Data;

/// <summary>
/// Contexto do banco do LabPoint. Esta classe é a versão PostgreSQL (migrations em <c>Backend/Migrations</c>);
/// <see cref="SqliteAppDbContext"/> herda dela para o SQLite (migrations em <c>Backend/Migrations/Sqlite</c>).
/// O provedor é escolhido em <c>Database:Provider</c> no appsettings (ver <see cref="DatabaseSetup"/>).
/// </summary>
public class AppDbContext : IdentityDbContext<AccountModel>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>Para contextos derivados (um por provedor), que têm opções e migrations próprias.</summary>
    protected AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<SubjectModel> Subjects { get; set; }

    public DbSet<AccountSubjectModel> AccountSubjects { get; set; }

    public DbSet<ResourceModel>  Resources { get; set; }

    public DbSet<SpaceModel> Spaces { get; set; }

    public DbSet<SpaceSubjectModel> SpaceSubjects { get; set; }

    public DbSet<SpaceResourceModel> SpaceResources { get; set; }

    public DbSet<SpaceReserveModel> SpaceReserves { get; set; }

    public DbSet<ScheduleModel> Schedules { get; set; }

    public DbSet<SpaceReserveScheduleModel>  SpaceReserveSchedules { get; set; }

    public DbSet<ResourceReserveModel> ResourceReserves { get; set; }

    public DbSet<ResourceReserveScheduleModel> ResourceReserveSchedules { get; set; }

    public DbSet<ReportModel> Reports { get; set; }

    public DbSet<ReserveUsageModel> ReserveUsages { get; set; }

    public DbSet<ReserveUsageEntryModel> ReserveUsageEntries { get; set; }

    public DbSet<HolidayModel> Holidays { get; set; }

    public DbSet<HolidayYearSyncModel> HolidayYearSyncs { get; set; }

    public DbSet<ReleasedDateModel> ReleasedDates { get; set; }

    public DbSet<EmailOutboxModel> EmailOutbox { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Recursos exclusivos do PostgreSQL: id uuidv7 gerado pelo banco e índices de busca full-text.
        // No SQLite o Identity gera o id (Guid) e a busca é feita só por trecho (LIKE).
        if (Database.IsNpgsql())
        {
            builder.Entity<AccountModel>().Property(a => a.Id).HasDefaultValueSql("uuidv7()");

            builder.Entity<AccountModel>()
                .HasIndex(a => new { a.Registration, a.UserName, a.Email })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("portuguese");

            builder.Entity<SpaceModel>()
                .HasIndex(a => new { a.Name, a.Description })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("portuguese");

            builder.Entity<ResourceModel>()
                .HasIndex(r => new { r.Name, r.Description })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("portuguese");
        }

        // Consultas de disponibilidade: reservas de um recurso que se sobrepõem a um período.
        builder.Entity<ResourceReserveModel>()
            .HasIndex(r => new { r.FkResourceId, r.DateFrom, r.DateTo });

        // Sessão de uso: de uma reserva de sala OU de uma reserva de recurso avulsa (nunca as duas).
        builder.Entity<ReserveUsageModel>()
            .ToTable(t => t.HasCheckConstraint(
                "ck_reserve_usage_single_reserve",
                "(fk_space_reserve_id IS NULL) <> (fk_resource_reserve_id IS NULL)"));

        // Duas FKs para a conta: o dono da reserva e o admin que fechou o check-out.
        builder.Entity<ReserveUsageModel>()
            .HasOne(u => u.Account)
            .WithMany()
            .HasForeignKey(u => u.FkAccountId);

        builder.Entity<ReserveUsageModel>()
            .HasOne(u => u.CheckedOutBy)
            .WithMany()
            .HasForeignKey(u => u.FkCheckedOutById)
            .OnDelete(DeleteBehavior.SetNull);

        // Aviso de check-in/check-out na fila de e-mails: se a sessão for excluída, o registro do envio continua.
        builder.Entity<EmailOutboxModel>()
            .HasOne(e => e.Usage)
            .WithMany(u => u.Emails)
            .HasForeignKey(e => e.FkUsageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
