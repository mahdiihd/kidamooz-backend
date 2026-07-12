using Kidamooz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> Users => Set<AdminUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CatalogMeta> CatalogMeta => Set<CatalogMeta>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<StoryChapter> StoryChapters => Set<StoryChapter>();
    public DbSet<AudienceSegment> AudienceSegments => Set<AudienceSegment>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<StoryAudienceSegment> StoryAudienceSegments => Set<StoryAudienceSegment>();
    public DbSet<StoryAudienceUser> StoryAudienceUsers => Set<StoryAudienceUser>();
    public DbSet<StoryViewsDaily> StoryViewsDaily => Set<StoryViewsDaily>();
    public DbSet<AppOpensDaily> AppOpensDaily => Set<AppOpensDaily>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.PasswordHash).HasMaxLength(512);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Role).HasMaxLength(50).HasDefaultValue("editor");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.TokenHash).HasMaxLength(256);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<CatalogMeta>(e =>
        {
            e.ToTable("catalog_meta");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Version).HasMaxLength(128);
            e.ToTable(t => t.HasCheckConstraint("CK_catalog_meta_singleton", "[Id] = 1"));
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.HasIndex(x => x.Slug).IsUnique().HasFilter("[DeletedAt] IS NULL");
            e.Property(x => x.Slug).HasMaxLength(64);
            e.Property(x => x.TitleFa).HasMaxLength(300);
            e.Property(x => x.TitleEn).HasMaxLength(300);
            e.Property(x => x.IconUrl).HasMaxLength(1000);
            e.Property(x => x.Color).HasMaxLength(16);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasIndex(x => x.SortOrder).HasFilter("[DeletedAt] IS NULL");
        });

        modelBuilder.Entity<Story>(e =>
        {
            e.ToTable("stories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.TitleFa).HasMaxLength(300);
            e.Property(x => x.TitleEn).HasMaxLength(300);
            e.Property(x => x.DescriptionFa).HasMaxLength(2000);
            e.Property(x => x.DescriptionEn).HasMaxLength(2000);
            e.Property(x => x.CoverUrl).HasMaxLength(1000);
            e.Property(x => x.AudioUrl).HasMaxLength(1000);
            e.Property(x => x.Visibility).HasMaxLength(20).HasDefaultValue("public");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.ToTable(t => t.HasCheckConstraint("CK_stories_visibility", "[Visibility] IN ('public', 'restricted')"));
            e.HasOne(x => x.Category).WithMany(x => x.Stories).HasForeignKey(x => x.CategoryId);
            e.HasIndex(x => x.CategoryId).HasFilter("[DeletedAt] IS NULL");
            e.HasIndex(x => x.SortOrder).HasFilter("[DeletedAt] IS NULL");
            e.HasIndex(x => x.Published).HasFilter("[DeletedAt] IS NULL");
        });

        modelBuilder.Entity<StoryChapter>(e =>
        {
            e.ToTable("story_chapters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.StoryId).HasMaxLength(64);
            e.Property(x => x.TitleFa).HasMaxLength(300);
            e.Property(x => x.TitleEn).HasMaxLength(300);
            e.Property(x => x.ImageUrl).HasMaxLength(1000);
            e.HasOne(x => x.Story).WithMany(x => x.Chapters).HasForeignKey(x => x.StoryId);
            e.HasIndex(x => new { x.StoryId, x.SortOrder });
        });

        modelBuilder.Entity<AudienceSegment>(e =>
        {
            e.ToTable("audience_segments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Label).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("app_users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(256);
        });

        modelBuilder.Entity<StoryAudienceSegment>(e =>
        {
            e.ToTable("story_audience_segments");
            e.HasKey(x => new { x.StoryId, x.SegmentId });
            e.Property(x => x.StoryId).HasMaxLength(64);
            e.Property(x => x.SegmentId).HasMaxLength(64);
            e.HasOne(x => x.Story).WithMany(x => x.AudienceSegments).HasForeignKey(x => x.StoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Segment).WithMany(x => x.StoryAudienceSegments).HasForeignKey(x => x.SegmentId);
        });

        modelBuilder.Entity<StoryAudienceUser>(e =>
        {
            e.ToTable("story_audience_users");
            e.HasKey(x => new { x.StoryId, x.UserId });
            e.Property(x => x.StoryId).HasMaxLength(64);
            e.Property(x => x.UserId).HasMaxLength(64);
            e.HasOne(x => x.Story).WithMany(x => x.AudienceUsers).HasForeignKey(x => x.StoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(x => x.StoryAudienceUsers).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<StoryViewsDaily>(e =>
        {
            e.ToTable("story_views_daily");
            e.HasKey(x => new { x.ViewDate, x.StoryId });
            e.Property(x => x.StoryId).HasMaxLength(64);
            e.HasOne(x => x.Story).WithMany().HasForeignKey(x => x.StoryId);
        });

        modelBuilder.Entity<AppOpensDaily>(e =>
        {
            e.ToTable("app_opens_daily");
            e.HasKey(x => x.ViewDate);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.EntityId).HasMaxLength(128);
            e.Property(x => x.EntityTitle).HasMaxLength(300);
            e.Property(x => x.ActorEmail).HasMaxLength(256);
            e.Property(x => x.Details).HasMaxLength(1000);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasIndex(x => x.CreatedAt).IsDescending();
        });
    }
}
