
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Data;

public partial class NhaKhoaDbContext : DbContext
{
    public NhaKhoaDbContext(
        DbContextOptions<NhaKhoaDbContext> options)
        : base(options)
    {
    }

    // ==========================================
    // CÁC BẢNG HIỆN TẠI
    // ==========================================

    public virtual DbSet<AdminUser> AdminUsers { get; set; }
    public virtual DbSet<Article> Articles { get; set; }
    public virtual DbSet<ArticleCategory> ArticleCategories { get; set; }
    public virtual DbSet<Branch> Branches { get; set; }
    public virtual DbSet<ChatFaq> ChatFaqs { get; set; }
    public virtual DbSet<ConsultationRequest> ConsultationRequests { get; set; }
    public virtual DbSet<Doctor> Doctors { get; set; }
    public virtual DbSet<MediaFile> MediaFiles { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }
    public virtual DbSet<ServiceFaq> ServiceFaqs { get; set; }
    public virtual DbSet<ServicePrice> ServicePrices { get; set; }
    public virtual DbSet<SitePage> SitePages { get; set; }
    public virtual DbSet<SiteSetting> SiteSettings { get; set; }

    // ==========================================
    // HỆ THỐNG THÔNG BÁO ADMIN
    // ==========================================

    public virtual DbSet<AdminNotification> AdminNotifications { get; set; }
    public virtual DbSet<AdminNotificationRead> AdminNotificationReads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ==========================================
        // 1. ADMIN USERS
        // ==========================================

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("AdminUsers");
            entity.HasKey(e => e.AdminId);

            entity.HasIndex(e => e.Username).IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        // ==========================================
        // 2. ARTICLE CATEGORIES
        // ==========================================

        modelBuilder.Entity<ArticleCategory>(entity =>
        {
            entity.ToTable("ArticleCategories");
            entity.HasKey(e => e.CategoryId);

            entity.HasIndex(e => e.Slug).IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Slug).HasMaxLength(160);
        });

        // ==========================================
        // 3. SERVICE CATEGORIES
        // ==========================================

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.ToTable("ServiceCategories");
            entity.HasKey(e => e.CategoryId);

            entity.HasIndex(e => e.Slug).IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Slug).HasMaxLength(160);
        });

        // ==========================================
        // 4. BRANCHES
        // ==========================================

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");
            entity.HasKey(e => e.BranchId);

            entity.Property(e => e.Address).HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.GoogleMapsUrl).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.OpeningHours).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        // ==========================================
        // 5. DOCTORS
        // ==========================================

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(e => e.DoctorId);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.PhotoUrl).HasMaxLength(1000);
            entity.Property(e => e.Specialty).HasMaxLength(200);
        });

        // ==========================================
        // 6. SERVICES
        // ==========================================

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("Services");
            entity.HasKey(e => e.ServiceId);

            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.IconClass).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.ShortDescription).HasMaxLength(1000);
            entity.Property(e => e.Slug).HasMaxLength(220);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(1000);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Services)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ==========================================
        // 7. ARTICLES
        // ==========================================

        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("Articles");
            entity.HasKey(e => e.ArticleId);

            entity.HasIndex(e => e.AuthorId);
            entity.HasIndex(e => new { e.CategoryId, e.Status });
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.Property(e => e.CoverImageUrl).HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.Slug).HasMaxLength(320);

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("draft");

            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.Author)
                .WithMany(p => p.Articles)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Articles)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ==========================================
        // 8. CHAT FAQS
        // ==========================================

        modelBuilder.Entity<ChatFaq>(entity =>
        {
            entity.ToTable("ChatFAQs");
            entity.HasKey(e => e.ChatFaqid);

            entity.HasIndex(e => e.ServiceId);

            entity.Property(e => e.ChatFaqid)
                .HasColumnName("ChatFAQId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Keywords).HasMaxLength(1000);
            entity.Property(e => e.Question).HasMaxLength(500);

            entity.HasOne(d => d.Service)
                .WithMany(p => p.ChatFaqs)
                .HasForeignKey(d => d.ServiceId);
        });

        // ==========================================
        // 9. CONSULTATION REQUESTS
        // ==========================================

        modelBuilder.Entity<ConsultationRequest>(entity =>
        {
            entity.ToTable("ConsultationRequests");
            entity.HasKey(e => e.ConsultationId);

            entity.HasIndex(e => e.ServiceId);

            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .IsDescending(false, true);

            entity.Property(e => e.Age)
                .HasConversion<short>()
                .HasColumnType("smallint");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Note).HasMaxLength(2000);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");

            entity.HasOne(d => d.Service)
                .WithMany(p => p.ConsultationRequests)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ==========================================
        // 10. MEDIA FILES
        // ==========================================

        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.ToTable("MediaFiles");
            entity.HasKey(e => e.MediaId);

            entity.Property(e => e.ContentType).HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.PublicUrl).HasMaxLength(1500);
            entity.Property(e => e.StorageKey).HasMaxLength(1000);

            entity.Property(e => e.StorageProvider)
                .HasMaxLength(30)
                .HasDefaultValue("azure");

            entity.HasOne(d => d.UploadedByNavigation)
                .WithMany(p => p.MediaFiles)
                .HasForeignKey(d => d.UploadedBy);
        });

        // ==========================================
        // 11. SERVICE FAQS
        // ==========================================

        modelBuilder.Entity<ServiceFaq>(entity =>
        {
            entity.ToTable("ServiceFAQs");
            entity.HasKey(e => e.Faqid);

            entity.HasIndex(e => e.ServiceId);

            entity.Property(e => e.Faqid)
                .HasColumnName("FAQId");

            entity.Property(e => e.Question).HasMaxLength(500);

            entity.HasOne(d => d.Service)
                .WithMany(p => p.ServiceFaqs)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ==========================================
        // 12. SERVICE PRICES
        // ==========================================

        modelBuilder.Entity<ServicePrice>(entity =>
        {
            entity.ToTable("ServicePrices");
            entity.HasKey(e => e.PriceId);

            entity.HasIndex(e => e.ServiceId);

            entity.Property(e => e.Amount)
                .HasColumnType("numeric(18,2)");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.PriceCode).HasMaxLength(30);
            entity.Property(e => e.PriceName).HasMaxLength(200);
            entity.Property(e => e.Unit).HasMaxLength(50);

            entity.HasOne(d => d.Service)
                .WithMany(p => p.ServicePrices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ==========================================
        // 13. SITE PAGES
        // ==========================================

        modelBuilder.Entity<SitePage>(entity =>
        {
            entity.ToTable("SitePages");
            entity.HasKey(e => e.PageId);

            entity.HasIndex(e => e.Slug).IsUnique();

            entity.Property(e => e.BannerUrl).HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.Property(e => e.Slug).HasMaxLength(250);
            entity.Property(e => e.Summary).HasMaxLength(1500);
            entity.Property(e => e.Title).HasMaxLength(250);
        });

        // ==========================================
        // 14. SITE SETTINGS
        // ==========================================

        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.ToTable("SiteSettings");
            entity.HasKey(e => e.SettingId);

            entity.HasIndex(e => e.SettingKey).IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SettingKey).HasMaxLength(120);

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.HasOne(d => d.UpdatedByNavigation)
                .WithMany(p => p.SiteSettings)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ==========================================
        // 15. ADMIN NOTIFICATIONS (NEW)
        // ==========================================

        modelBuilder.Entity<AdminNotification>(entity =>
        {
            entity.ToTable("AdminNotifications");

            entity.HasKey(e => e.NotificationId);

            entity.HasIndex(e => e.ConsultationId)
                .IsUnique();

            entity.HasIndex(e => e.CreatedAt)
                .IsDescending();

            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValue("new_consultation");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.HasOne(e => e.Consultation)
                .WithMany()
                .HasForeignKey(e => e.ConsultationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // 16. ADMIN NOTIFICATION READS (NEW)
        // ==========================================

        modelBuilder.Entity<AdminNotificationRead>(entity =>
        {
            entity.ToTable("AdminNotificationReads");

            entity.HasKey(e => new
            {
                e.NotificationId,
                e.AdminId
            });

            entity.Property(e => e.ReadAt)
                .HasDefaultValueSql("(now() at time zone 'UTC')");

            entity.HasOne(e => e.Notification)
                .WithMany(n => n.Reads)
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Admin)
                .WithMany()
                .HasForeignKey(e => e.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // 17. POSTGRESQL TIMESTAMP COMPATIBILITY
        // ==========================================

        // Neon lưu thời gian dưới dạng:
        // timestamp without time zone.
        //
        // Ứng dụng sử dụng quy ước UTC.
        // Giữ nguyên converter đang dùng để
        // không ảnh hưởng các tính năng cũ.

        var dateTimeConverter =
            new ValueConverter<DateTime, DateTime>(
                value => DateTime.SpecifyKind(
                    value, DateTimeKind.Unspecified),
                value => DateTime.SpecifyKind(
                    value, DateTimeKind.Utc));

        var nullableDateTimeConverter =
            new ValueConverter<DateTime?, DateTime?>(
                value => value.HasValue
                    ? DateTime.SpecifyKind(
                        value.Value, DateTimeKind.Unspecified)
                    : null,
                value => value.HasValue
                    ? DateTime.SpecifyKind(
                        value.Value, DateTimeKind.Utc)
                    : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetColumnType(
                        "timestamp without time zone");

                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType(
                        "timestamp without time zone");

                    property.SetValueConverter(
                        nullableDateTimeConverter);
                }
            }
        }

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
