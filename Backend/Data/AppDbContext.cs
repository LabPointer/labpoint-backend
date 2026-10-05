using Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Models.Account;
using Models.Reserve;
using Models.Resource;
using Models.Schedule;
using Models.Subject;

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
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<AccountModel>().Property(a => a.Id).HasDefaultValueSql("uuidv7()");
        
        builder.Entity<AccountModel>()
            .HasGeneratedTsVectorColumn(
                a => a.SearchVector,
                "portuguese",
                a => new { a.Registration, a.UserName, a.Email }) 
            .HasIndex(a => a.SearchVector)
            .HasMethod("GIN"); 
        
        builder.Entity<ResourceModel>()
            .HasGeneratedTsVectorColumn(
                r => r.SearchVector,
                "portuguese",
                r => new { r.Name, r.Description }) 
            .HasIndex(r => r.SearchVector)
            .HasMethod("GIN");
        
        builder.Entity<SpaceModel>()
            .HasGeneratedTsVectorColumn(
                s => s.SearchVector,
                "portuguese",
                s => new { s.Name, s.Description }) 
            .HasIndex(s => s.SearchVector)
            .HasMethod("GIN"); 
    }
}