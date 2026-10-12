using Hero.Models;
using Hero3.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

        //public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<ApprovalFlow> ApprovalFlows { get; set; }
    public DbSet<ApplicationUser> ApplicationUser { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    base.OnConfiguring(optionsBuilder);

    //    optionsBuilder.UseSqlServer("Server=rawan;Database=Hero;Trusted_Connection=True;TrustServerCertificate=True");
    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.LineManager)
            .WithMany()
            .HasForeignKey(u => u.LineManagerId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        modelBuilder.Entity<ApprovalFlow>()
            .HasOne(af => af.leaveRequest)
            .WithMany()
            .HasForeignKey(af => af.LeaveRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne(l => l.ApplicationUser)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }





}

