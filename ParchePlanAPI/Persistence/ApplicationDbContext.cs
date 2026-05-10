using ParchePlanAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ParchePlanAPI.Persistence;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Parche> Parches { get; set; }
    public DbSet<ParcheMember> ParcheMembers { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<PlanOption> PlanOptions { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlanOption>()
            .HasOne(po => po.Plan)
            .WithMany()
            .HasForeignKey(po => po.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Plan>()
            .HasOne(p => p.WinningOption)
            .WithMany()
            .HasForeignKey(p => p.WinningOptionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
    
}
