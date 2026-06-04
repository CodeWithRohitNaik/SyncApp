using Microsoft.EntityFrameworkCore;
using ThrustSync.Core.Models;

namespace ThrustSync.Data.Context;

/// <summary>
/// EF Core DbContext for TrustSync application
/// Manages database operations for WorkOrders, APUs, KPI entries, and audit logs
/// </summary>
public class TrustSyncDbContext : DbContext
{
    public TrustSyncDbContext(DbContextOptions<TrustSyncDbContext> options) : base(options)
    {
    }

    /// <summary>DbSet for WorkOrders</summary>
    public DbSet<WorkOrder> WorkOrders { get; set; }

    /// <summary>DbSet for APUs</summary>
    public DbSet<APU> APUs { get; set; }

    /// <summary>DbSet for Oracle snapshots</summary>
    public DbSet<OracleSnapshot> OracleSnapshots { get; set; }

    /// <summary>DbSet for KPI entries</summary>
    public DbSet<KpiEntry> KpiEntries { get; set; }

    /// <summary>DbSet for audit logs</summary>
    public DbSet<AuditLog> AuditLogs { get; set; }

    /// <summary>DbSet for export logs</summary>
    public DbSet<ExportLog> ExportLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WorkOrder configuration
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.JCN).IsUnique();
            entity.HasIndex(e => e.FRACPR);
            entity.HasIndex(e => e.MID);
            entity.HasIndex(e => e.TailNumber);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => e.OraclePulledOn);

            entity.Property(e => e.JCN)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.FRACPR)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.MID)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.TailNumber)
                .HasMaxLength(50);

            // Relationships
            entity.HasOne(e => e.APU)
                .WithOne(a => a.WorkOrder)
                .HasForeignKey<APU>(a => a.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.OracleSnapshot)
                .WithOne(os => os.WorkOrder)
                .HasForeignKey<OracleSnapshot>(os => os.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.KpiEntries)
                .WithOne(k => k.WorkOrder)
                .HasForeignKey(k => k.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AuditLogs)
                .WithOne(a => a.WorkOrder)
                .HasForeignKey(a => a.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // APU configuration
        modelBuilder.Entity<APU>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.RefDes);
            entity.HasIndex(e => e.PartNumber);
            entity.HasIndex(e => e.PartSerialNumber);
            entity.HasIndex(e => new { e.RefDes, e.PartNumber, e.PartSerialNumber }).IsUnique();

            entity.Property(e => e.RefDes)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.PartNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PartSerialNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.RemovalIndicator)
                .HasMaxLength(10);

            entity.Property(e => e.ReviewStatus)
                .IsRequired()
                .HasMaxLength(10);
        });

        // OracleSnapshot configuration
        modelBuilder.Entity<OracleSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.PulledOn);
            entity.HasIndex(e => e.WorkOrderId).IsUnique();

            entity.Property(e => e.SourceData)
                .IsRequired()
                .HasColumnType("nvarchar(max)");
        });

        // KpiEntry configuration
        modelBuilder.Entity<KpiEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.WorkOrderId);
            entity.HasIndex(e => e.EnteredOn);

            entity.Property(e => e.EnteredBy)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.FlightHours)
                .HasPrecision(18, 2);

            entity.Property(e => e.OtherHours)
                .HasPrecision(18, 2);

            entity.Property(e => e.OpTime)
                .HasPrecision(18, 2);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.WorkOrderId);
            entity.HasIndex(e => e.ChangedOn);
            entity.HasIndex(e => e.ChangedBy);

            entity.Property(e => e.ChangeType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.FieldName)
                .HasMaxLength(100);

            entity.Property(e => e.OldValue)
                .HasMaxLength(500);

            entity.Property(e => e.NewValue)
                .HasMaxLength(500);

            entity.Property(e => e.ChangedBy)
                .IsRequired()
                .HasMaxLength(256);
        });

        // ExportLog configuration
        modelBuilder.Entity<ExportLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.ExportedOn);
            entity.HasIndex(e => e.ExportedBy);

            entity.Property(e => e.ExportedBy)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.FilterCriteria)
                .HasMaxLength(1000);
        });
    }
}
