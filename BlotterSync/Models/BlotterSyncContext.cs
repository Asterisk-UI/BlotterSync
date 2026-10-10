using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BlotterSync.Models;

public partial class BlotterSyncContext : DbContext
{
    public BlotterSyncContext()
    {
    }

    public BlotterSyncContext(DbContextOptions<BlotterSyncContext> options)
        : base(options)
    {
    }

    // Lets the local (SQLite) and cloud (Supabase) stores derive from this context
    // so each provider gets its own cached model.
    protected BlotterSyncContext(DbContextOptions options)
        : base(options)
    {
    }

    public virtual DbSet<BlotterRecord> BlotterRecords { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Officer> Officers { get; set; }

    public virtual DbSet<Resident> Residents { get; set; }
    public DbSet<Announcement> Announcements { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var envConn = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(envConn))
            {
                optionsBuilder.UseNpgsql(envConn);
            }
            else
            {
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=BlotterSyncDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var isPostgreSql = Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        var isSqlite = Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

        modelBuilder.Entity<BlotterRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__BlotterR__FBDF78E9C14F26A9");

            entity.HasIndex(e => e.TrackingNumber, "UQ__BlotterR__784DB3D9D658DE32").IsUnique();

            if (isPostgreSql)
            {
                entity.Property(e => e.IncidentDate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.ReportedDate)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp without time zone");
                entity.Property(e => e.ResolutionDate).HasColumnType("timestamp without time zone");
            }
            else if (isSqlite)
            {
                entity.Property(e => e.ReportedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
            else
            {
                entity.Property(e => e.IncidentDate).HasColumnType("datetime");
                entity.Property(e => e.ReportedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.ResolutionDate).HasColumnType("datetime");
            }

            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TrackingNumber).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.BlotterRecords)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BlotterRe__Categ__3E52440B");

            entity.HasOne(d => d.Complainant).WithMany(p => p.BlotterRecordComplainants)
                .HasForeignKey(d => d.ComplainantId)
                .HasConstraintName("FK__BlotterRe__Compl__4BAC3F29");

            entity.HasOne(d => d.DeskOfficer).WithMany(p => p.BlotterRecords)
                .HasForeignKey(d => d.DeskOfficerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BlotterRe__DeskO__3F466844");

            entity.HasOne(d => d.Respondent).WithMany(p => p.BlotterRecordRespondents)
                .HasForeignKey(d => d.RespondentId)
                .HasConstraintName("FK__BlotterRe__Respo__4CA06362");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B064FE278");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Officer>(entity =>
        {
            entity.HasKey(e => e.OfficerId).HasName("PK__Officers__2E65577A9E7C9A2C");

            entity.HasIndex(e => e.Username, "UQ_Officers_Username").IsUnique();

            entity.HasIndex(e => e.BadgeNumber, "UQ__Officers__D110FD56D35C9A0C").IsUnique();

            entity.Property(e => e.ActiveStatus).HasDefaultValue(true);
            entity.Property(e => e.BadgeNumber).HasMaxLength(50);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("Officer");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<Resident>(entity =>
        {
            entity.HasKey(e => e.ResidentId).HasName("PK__Resident__07FB00DC9C87A8DE");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.ContactNumber).HasMaxLength(20);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
        });

        if (isPostgreSql)
        {
            modelBuilder.Entity<Announcement>(entity =>
            {
                entity.Property(e => e.DatePosted).HasColumnType("timestamp without time zone");
            });
        }

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
