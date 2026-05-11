using System;
using System.Collections.Generic;
using CommunicationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommunicationHub.Infrastructure.Data;

public partial class CommunicationHubDbContext : DbContext
{
    public CommunicationHubDbContext()
    {
    }

    public CommunicationHubDbContext(DbContextOptions<CommunicationHubDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Adjuster> Adjusters { get; set; }

    public virtual DbSet<Channel> Channels { get; set; }

    public virtual DbSet<Claim> Claims { get; set; }

    public virtual DbSet<ClaimAdjuster> ClaimAdjusters { get; set; }

    public virtual DbSet<Communication> Communications { get; set; }

    public virtual DbSet<InvolvedParty> InvolvedParties { get; set; }

    public virtual DbSet<MessageAttachment> MessageAttachments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=CommunicationHubDB;Trusted_Connection=True;TrustServerCertificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Adjuster>(entity =>
        {
            entity.ToTable("Adjuster");
            entity.HasKey(e => e.AdjusterId).HasName("PK__Adjuster__C81DA416CE19FCC2");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.ToTable("Channel");
            entity.HasKey(e => e.ChannelId).HasName("PK__Channel__38C3E8142D4EADAA");
        });

        modelBuilder.Entity<Channel>().HasData(
            new Channel { ChannelId = 1, Name = "Email", IsActive = true },
            new Channel { ChannelId = 2, Name = "Sms", IsActive = true },
            new Channel { ChannelId = 3, Name = "WhatsApp", IsActive = true }
        );

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.ToTable("Claim");
            entity.HasKey(e => e.ClaimId).HasName("PK__Claim__EF2E139B262E360E");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<ClaimAdjuster>(entity =>
        {
            entity.ToTable("ClaimAdjuster");
            entity.HasKey(e => e.ClaimAdjusterId).HasName("PK__ClaimAdj__599F82BA9EB00B8A");

            entity.HasIndex(e => new { e.ClaimId, e.AdjusterId }, "UX_ClaimAdjuster_Active")
                .IsUnique()
                .HasFilter("([UnassignedAt] IS NULL)");

            entity.HasIndex(e => e.ClaimId, "UX_ClaimAdjuster_Primary")
                .IsUnique()
                .HasFilter("([IsPrimary]=(1) AND [UnassignedAt] IS NULL)");

            entity.HasOne(d => d.Adjuster).WithMany(p => p.ClaimAdjusters).HasConstraintName("FK__ClaimAdju__Adjus__5165187F");

            entity.HasOne(d => d.Claim).WithOne(p => p.ClaimAdjuster).HasConstraintName("FK__ClaimAdju__Claim__5070F446");
        });

        modelBuilder.Entity<Communication>(entity =>
        {
            entity.ToTable("Communication");
            entity.HasKey(e => e.CommunicationId).HasName("PK__Communic__53E565EF80F32ECE");

            entity.HasIndex(e => new { e.ClaimId, e.PartyId, e.ChannelId }, "UX_Communication_ActiveThread")

                .HasFilter("([IsActive]=(1))");

            entity.Property(e => e.CommunicationId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Sid).HasMaxLength(100);

            entity.HasOne(d => d.Adjuster).WithMany(p => p.Communications).HasConstraintName("FK__Communica__Adjus__5DCAEF64");

            entity.HasOne(d => d.Channel).WithMany(p => p.Communications).HasConstraintName("FK__Communica__Chann__5CD6CB2B");

            entity.HasOne(d => d.Claim).WithMany(p => p.Communications).HasConstraintName("FK__Communica__Claim__5AEE82B9");

            entity.HasOne(d => d.Party).WithMany(p => p.Communications).HasConstraintName("FK__Communica__Party__5BE2A6F2");
        });

        modelBuilder.Entity<InvolvedParty>(entity =>
        {
            entity.ToTable("InvolvedParty");
            entity.HasKey(e => e.PartyId).HasName("PK__Involved__1640CD33FF54187F");

            entity.HasOne(d => d.Claim).WithMany(p => p.InvolvedParties).HasConstraintName("FK__InvolvedP__Claim__5441852A");
        });

        modelBuilder.Entity<MessageAttachment>(entity =>
        {
            entity.ToTable("MessageAttachment");
            entity.HasKey(e => e.AttachmentId).HasName("PK__MessageA__442C64BE7AE67570");

            entity.HasIndex(e => e.CommunicationId, "IX_MessageAttachment_CommunicationId");

            entity.Property(e => e.AttachmentId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.S3Key).HasMaxLength(500);
            entity.Property(e => e.FileName).HasMaxLength(255);

            entity.HasOne(d => d.Communication).WithMany(p => p.MessageAttachments).HasConstraintName("FK__MessageAt__Commu__628FA481");
        });

        // Use UTC for all DateTime properties
        var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()),
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
