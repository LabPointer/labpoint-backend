using Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Models.Account;
using Models.Report;
using Models.Reserve;
using Models.Resource;
using Models.Schedule;
using Models.Subject;
using Models.Usage;

namespace Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : IdentityDbContext<AccountModel>(options)
{
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
    }
}