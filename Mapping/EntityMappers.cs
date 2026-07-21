using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Domain;

namespace Kidamooz.Mapping;

public static class EntityMappers
{
    public static LocalizedTextDto ToDto(string fa, string en) => new(fa, en);

    public static StoryAccessDto ToAccessDto(Story story) => new(
        story.Visibility,
        new StoryAudienceDto(
            story.AudienceSegments.Select(x => x.SegmentId).ToList(),
            story.AudienceUsers.Select(x => x.UserId).ToList()));

    public static StoryDto ToStoryDto(Story story) => new(
        story.Id,
        ToDto(story.TitleFa, story.TitleEn),
        ToDto(story.DescriptionFa, story.DescriptionEn),
        story.CoverUrl,
        story.AudioUrl,
        story.DurationSeconds,
        story.AgeMin,
        story.AgeMax,
        story.CategoryId,
        ProgressIcons.Normalize(story.ProgressIcon),
        story.Featured,
        story.SortOrder,
        story.Published,
        story.PublishedAt,
        ToAccessDto(story),
        story.AuthorName);

    public static StoryDetailDto ToStoryDetailDto(Story story) => new(
        story.Id,
        ToDto(story.TitleFa, story.TitleEn),
        ToDto(story.DescriptionFa, story.DescriptionEn),
        story.CoverUrl,
        story.AudioUrl,
        story.DurationSeconds,
        story.AgeMin,
        story.AgeMax,
        story.CategoryId,
        ProgressIcons.Normalize(story.ProgressIcon),
        story.Featured,
        story.SortOrder,
        story.Published,
        story.PublishedAt,
        ToAccessDto(story),
        story.Chapters.OrderBy(c => c.SortOrder).Select(ToChapterDto).ToList(),
        story.AuthorName);

    public static StoryChapterDto ToChapterDto(StoryChapter chapter) => new(
        ToDto(chapter.TitleFa, chapter.TitleEn),
        chapter.StartSeconds,
        chapter.ImageUrl);

    public static CategoryDto ToCategoryDto(Category category) => new(
        category.Id,
        ToDto(category.TitleFa, category.TitleEn),
        category.Slug,
        category.IconUrl,
        category.Color,
        category.SortOrder,
        category.Published);

    public static CatalogVersionDto ToCatalogVersionDto(CatalogMeta meta) =>
        new(meta.Version, meta.UpdatedAt);

    public static AuditLogDto ToAuditLogDto(AuditLog log) => new(
        log.Id.ToString(),
        log.Action,
        log.EntityType,
        log.EntityId,
        log.EntityTitle,
        log.ActorEmail,
        log.CreatedAt,
        log.Details);

    public static AudienceSegmentDto ToAudienceSegmentDto(AudienceSegment segment) =>
        new(segment.Id, segment.Label, segment.Description);

    public static AudienceUserDto ToAudienceUserDto(AppUser user) =>
        new(user.Id, user.DisplayName, user.Email);

    public static PublicStoryDto ToPublicStoryDto(Story story) => new(
        story.Id,
        story.TitleFa,
        story.TitleFa,
        story.TitleEn,
        story.DescriptionFa,
        story.DescriptionFa,
        story.DescriptionEn,
        story.CoverUrl,
        story.AudioUrl,
        story.DurationSeconds,
        story.AgeMin,
        story.AgeMax,
        story.CategoryId,
        ProgressIcons.Normalize(story.ProgressIcon),
        story.Featured,
        story.SortOrder,
        story.Published,
        story.PublishedAt,
        story.AuthorName);

    public static PublicStoryDetailDto ToPublicStoryDetailDto(Story story) => new(
        story.Id,
        story.TitleFa,
        story.TitleFa,
        story.TitleEn,
        story.DescriptionFa,
        story.DescriptionFa,
        story.DescriptionEn,
        story.CoverUrl,
        story.AudioUrl,
        story.DurationSeconds,
        story.AgeMin,
        story.AgeMax,
        story.CategoryId,
        ProgressIcons.Normalize(story.ProgressIcon),
        story.Featured,
        story.SortOrder,
        story.Published,
        story.PublishedAt,
        story.Chapters.OrderBy(c => c.SortOrder).Select(ToChapterDto).ToList(),
        story.AuthorName);
}
