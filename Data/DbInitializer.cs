using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            var seedEmail = "admin@kidamooz.com";
            var seedPassword = Environment.GetEnvironmentVariable("KIDAMOOZ_SEED_ADMIN_PASSWORD") ?? "admin123";

            await AdminUserProvisioner.CreateAsync(
                db,
                seedEmail,
                seedPassword,
                "مدیر سیستم",
                "admin");
        }

        if (!await db.CatalogMeta.AnyAsync())
        {
            var now = DateTimeOffset.UtcNow;
            db.CatalogMeta.Add(new CatalogMeta
            {
                Id = 1,
                Version = $"{now:O}-0-0",
                UpdatedAt = now
            });
        }

        if (!await db.AudienceSegments.AnyAsync())
        {
            db.AudienceSegments.AddRange(
                new AudienceSegment { Id = "premium", Label = "اشتراک ویژه", Description = "کاربران با اشتراک پریمیوم" },
                new AudienceSegment { Id = "family", Label = "پلن خانوادگی", Description = "خانواده‌های با چند پروفایل کودک" },
                new AudienceSegment { Id = "beta", Label = "تسترهای بتا", Description = "گروه تست داخلی" },
                new AudienceSegment { Id = "school", Label = "مدارس همکار", Description = "مدارس طرف قرارداد" }
            );
        }

        if (!await db.AppUsers.AnyAsync())
        {
            db.AppUsers.AddRange(
                new AppUser { Id = "u-1001", DisplayName = "سارا احمدی", Email = "sara@example.com" },
                new AppUser { Id = "u-1002", DisplayName = "علی رضایی", Email = "ali@example.com" }
            );
        }

        if (!await db.Categories.AnyAsync(x => x.Id == Services.StoryDraftService.PersonalCategoryId))
        {
            var now = DateTimeOffset.UtcNow;
            db.Categories.Add(new Category
            {
                Id = Services.StoryDraftService.PersonalCategoryId,
                Slug = "personal",
                TitleFa = "قصه‌های من",
                TitleEn = "My Stories",
                IconUrl = string.Empty,
                Color = "#7bc950",
                SortOrder = 999,
                Published = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync();
    }
}
