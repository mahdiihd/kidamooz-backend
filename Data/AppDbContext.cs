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
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StoryDraft> StoryDrafts => Set<StoryDraft>();
    public DbSet<ChildProfile> ChildProfiles => Set<ChildProfile>();
    public DbSet<MemberFavorite> MemberFavorites => Set<MemberFavorite>();
    public DbSet<StoryOfTheDay> StoriesOfTheDay => Set<StoryOfTheDay>();
    public DbSet<WeeklyChallenge> WeeklyChallenges => Set<WeeklyChallenge>();

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
            e.Property(x => x.ProgressIcon).HasMaxLength(32).HasDefaultValue("star");
            e.Property(x => x.Visibility).HasMaxLength(20).HasDefaultValue("public");
            e.Property(x => x.AuthorName).HasMaxLength(200);
            e.Property(x => x.AuthorUserId).HasMaxLength(64);
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
            e.Property(x => x.Mobile).HasMaxLength(20);
            e.Property(x => x.PasswordHash).HasMaxLength(200);
            e.Property(x => x.PlanTier).HasMaxLength(20).HasDefaultValue(MemberPlans.Free);
            e.Property(x => x.LastPlayedStoryId).HasMaxLength(64);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasIndex(x => x.Mobile).IsUnique().HasFilter("[Mobile] IS NOT NULL");
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

        modelBuilder.Entity<DeviceToken>(e =>
        {
            e.ToTable("device_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Token).HasMaxLength(512);
            e.Property(x => x.Platform).HasMaxLength(32);
            e.Property(x => x.UserId).HasMaxLength(64);
            e.Property(x => x.AppVersion).HasMaxLength(64);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.LastSeenAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.Platform, x.IsActive });
            e.HasIndex(x => x.UserId);
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

        modelBuilder.Entity<StoryDraft>(e =>
        {
            e.ToTable("story_drafts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.DeviceId).HasMaxLength(128);
            e.Property(x => x.UserId).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.DrawingUrl).HasMaxLength(1000);
            e.Property(x => x.CoverUrl).HasMaxLength(1000);
            e.Property(x => x.CoverPrompt).HasMaxLength(1000);
            e.Property(x => x.ChallengeTag).HasMaxLength(64);
            e.Property(x => x.TitleFa).HasMaxLength(300);
            e.Property(x => x.DescriptionFa).HasMaxLength(2000);
            e.Property(x => x.StoryScript).HasMaxLength(8000);
            e.Property(x => x.AudioUrl).HasMaxLength(1000);
            e.Property(x => x.PublishedStoryId).HasMaxLength(64);
            e.Property(x => x.ErrorMessage).HasMaxLength(500);
            e.Property(x => x.RejectReason).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasOne(x => x.User).WithMany(x => x.StoryDrafts).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.DeviceId, x.UpdatedAt });
            e.HasIndex(x => new { x.UserId, x.UpdatedAt });
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<ChildProfile>(e =>
        {
            e.ToTable("child_profiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.UserId).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.AvatarKey).HasMaxLength(32);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasOne(x => x.User).WithMany(x => x.Children).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<MemberFavorite>(e =>
        {
            e.ToTable("member_favorites");
            e.HasKey(x => new { x.UserId, x.StoryId });
            e.Property(x => x.UserId).HasMaxLength(64);
            e.Property(x => x.StoryId).HasMaxLength(64);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasOne(x => x.User).WithMany(x => x.Favorites).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Story).WithMany().HasForeignKey(x => x.StoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoryOfTheDay>(e =>
        {
            e.ToTable("stories_of_the_day");
            e.HasKey(x => x.PickDate);
            e.Property(x => x.StoryId).HasMaxLength(64);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasOne(x => x.Story).WithMany().HasForeignKey(x => x.StoryId);
        });

        modelBuilder.Entity<WeeklyChallenge>(e =>
        {
            e.ToTable("weekly_challenges");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.TitleFa).HasMaxLength(200);
            e.Property(x => x.ThemeTag).HasMaxLength(64);
            e.Property(x => x.DescriptionFa).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.HasIndex(x => new { x.IsActive, x.WeekStart });
        });
    }
}
