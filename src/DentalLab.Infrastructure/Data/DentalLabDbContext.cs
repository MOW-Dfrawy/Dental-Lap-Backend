using DentalLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Infrastructure.Data;

public class DentalLabDbContext : DbContext
{
    public DentalLabDbContext(DbContextOptions<DentalLabDbContext> options) : base(options) { }

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseItem> CaseItems => Set<CaseItem>();
    public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
    public DbSet<CaseStageHistory> CaseStageHistories => Set<CaseStageHistory>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Clinic
        modelBuilder.Entity<Clinic>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.ContactPerson).HasMaxLength(100);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Email).HasMaxLength(150);
            b.Property(x => x.Address).HasMaxLength(300);
            b.Property(x => x.City).HasMaxLength(100);
            b.HasIndex(x => x.Name);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // Patient
        modelBuilder.Entity<Patient>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            b.Property(x => x.Gender).HasMaxLength(20);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.HasOne(x => x.Clinic)
                .WithMany(c => c.Patients)
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.FullName);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // Case
        modelBuilder.Entity<Case>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.CaseNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.Priority).HasMaxLength(20);
            b.Property(x => x.ToothShade).HasMaxLength(50);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");

            b.HasIndex(x => x.CaseNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ReceivedDate);

            b.HasOne(x => x.Clinic)
                .WithMany(c => c.Cases)
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Patient)
                .WithMany(p => p.Cases)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.AssignedTechnician)
                .WithMany(t => t.Cases)
                .HasForeignKey(x => x.AssignedTechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.CurrentStage)
                .WithMany()
                .HasForeignKey(x => x.CurrentStageId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // CaseItem
        modelBuilder.Entity<CaseItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            b.Property(x => x.ToothNumber).HasMaxLength(20);
            b.Property(x => x.Material).HasMaxLength(100);
            b.Property(x => x.Shade).HasMaxLength(50);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Ignore(x => x.LineTotal);

            b.HasOne(x => x.Case)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // WorkflowStage
        modelBuilder.Entity<WorkflowStage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Description).HasMaxLength(500);
        });

        // CaseStageHistory
        modelBuilder.Entity<CaseStageHistory>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasOne(x => x.Case)
                .WithMany(c => c.StageHistory)
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Stage)
                .WithMany()
                .HasForeignKey(x => x.StageId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Technician)
                .WithMany()
                .HasForeignKey(x => x.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.CaseId, x.EnteredAt });
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // Technician
        modelBuilder.Entity<Technician>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            b.Property(x => x.Email).HasMaxLength(150);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Specialty).HasMaxLength(100);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");
            b.Ignore(x => x.AmountDue);

            b.HasIndex(x => x.InvoiceNumber).IsUnique();
            b.HasIndex(x => x.Status);

            b.HasOne(x => x.Case)
                .WithMany(c => c.Invoices)
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Clinic)
                .WithMany()
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // Payment
        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.Property(x => x.Method).IsRequired().HasMaxLength(50);
            b.Property(x => x.ReferenceNumber).HasMaxLength(100);
            b.Property(x => x.ReceivedBy).HasMaxLength(150);

            b.HasOne(x => x.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // Delivery
        modelBuilder.Entity<Delivery>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.RecipientName).HasMaxLength(150);
            b.Property(x => x.DeliveryAddress).HasMaxLength(300);
            b.Property(x => x.Courier).HasMaxLength(100);
            b.Property(x => x.TrackingNumber).HasMaxLength(100);

            b.HasOne(x => x.Case)
                .WithMany(c => c.Deliveries)
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.Status);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        // ActivityLog
        modelBuilder.Entity<ActivityLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Action).IsRequired().HasMaxLength(100);
            b.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
            b.Property(x => x.PerformedBy).HasMaxLength(150);
            b.Property(x => x.Role).HasMaxLength(50);
            b.Property(x => x.Details).HasMaxLength(2000);
            b.HasIndex(x => x.Timestamp);
        });
    }

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
