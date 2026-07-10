using System.Text.Json;
using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Mapping;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public interface IStoryService
{
    Task<StoryListResponseDto> GetAllAsync(StoryQuery query, CancellationToken ct = default);
    Task<StoryDetailDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<StoryDetailDto> CreateAsync(StoryPayloadDto payload, CancellationToken ct = default);
    Task<StoryDetailDto> CreateWithMediaAsync(
        StorySaveForm form,
        IReadOnlyDictionary<int, IFormFile> chapterImages,
        CancellationToken ct = default);
    Task<StoryDetailDto> UpdateAsync(string id, StoryPayloadDto payload, CancellationToken ct = default);
    Task<StoryDetailDto> UpdateWithMediaAsync(
        string id,
        StorySaveForm form,
        IReadOnlyDictionary<int, IFormFile> chapterImages,
        CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<StoryDto> PublishAsync(string id, bool published, CancellationToken ct = default);
    Task<StoryDto> ToggleFeaturedAsync(string id, bool featured, CancellationToken ct = default);
    Task<StoryDetailDto> UpdateChaptersAsync(string id, List<StoryChapterDto> chapters, CancellationToken ct = default);
    Task<List<StoryDto>> ReorderAsync(List<string> ids, CancellationToken ct = default);
}

public class StoryService(
    IStoryRepository repository,
    ICategoryRepository categoryRepository,
    ICatalogService catalogService,
    IAuditService auditService,
    IMediaService mediaService,
    AppDbContext db) : IStoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<StoryListResponseDto> GetAllAsync(StoryQuery query, CancellationToken ct = default)
    {
        var (items, total) = await repository.QueryAsync(query, ct);
        return new StoryListResponseDto(items.Select(EntityMappers.ToStoryDto).ToList(), total);
    }

    public async Task<StoryDetailDto> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var story = await repository.GetByIdAsync(id, ct: ct)
            ?? throw new KeyNotFoundException("قصه یافت نشد");
        return EntityMappers.ToStoryDetailDto(story);
    }

    public async Task<StoryDetailDto> CreateWithMediaAsync(
        StorySaveForm form,
        IReadOnlyDictionary<int, IFormFile> chapterImages,
        CancellationToken ct = default)
    {
        var coverUrl = await ResolveMediaUrlAsync(form.Cover, form.CoverUrl, "cover", required: true, ct);
        var audioUrl = await ResolveMediaUrlAsync(form.Audio, form.AudioUrl, "audio", required: true, ct);
        var access = ParseAccess(form.AccessJson);
        var chapters = await ResolveChaptersAsync(form.ChaptersJson, chapterImages, ct);

        var payload = new StoryPayloadDto(
            form.Id,
            new LocalizedTextDto(form.TitleFa, form.TitleEn),
            new LocalizedTextDto(form.DescriptionFa, form.DescriptionEn),
            coverUrl,
            audioUrl,
            form.DurationSeconds,
            form.AgeMin,
            form.AgeMax,
            form.CategoryId,
            form.Featured,
            form.SortOrder,
            form.Published,
            null,
            access);

        var created = await CreateAsync(payload, ct);
        if (chapters.Count > 0)
            return await UpdateChaptersAsync(created.Id, chapters, ct);

        return created;
    }

    public async Task<StoryDetailDto> UpdateWithMediaAsync(
        string id,
        StorySaveForm form,
        IReadOnlyDictionary<int, IFormFile> chapterImages,
        CancellationToken ct = default)
    {
        var existing = await repository.GetByIdAsync(id, ct: ct)
            ?? throw new KeyNotFoundException("قصه یافت نشد");

        var coverUrl = await ResolveMediaUrlAsync(
            form.Cover,
            string.IsNullOrWhiteSpace(form.CoverUrl) ? existing.CoverUrl : form.CoverUrl,
            "cover",
            required: true,
            ct);
        var audioUrl = await ResolveMediaUrlAsync(
            form.Audio,
            string.IsNullOrWhiteSpace(form.AudioUrl) ? existing.AudioUrl : form.AudioUrl,
            "audio",
            required: true,
            ct);
        var access = ParseAccess(form.AccessJson);
        var chapters = await ResolveChaptersAsync(form.ChaptersJson, chapterImages, ct);

        var payload = new StoryPayloadDto(
            form.Id,
            new LocalizedTextDto(form.TitleFa, form.TitleEn),
            new LocalizedTextDto(form.DescriptionFa, form.DescriptionEn),
            coverUrl,
            audioUrl,
            form.DurationSeconds,
            form.AgeMin,
            form.AgeMax,
            form.CategoryId,
            form.Featured,
            form.SortOrder,
            form.Published,
            existing.PublishedAt,
            access);

        await UpdateAsync(id, payload, ct);
        return await UpdateChaptersAsync(id, chapters, ct);
    }

    public async Task<StoryDetailDto> CreateAsync(StoryPayloadDto payload, CancellationToken ct = default)
    {
        var id = payload.Id ?? Guid.NewGuid().ToString("N")[..8];
        if (await repository.ExistsAsync(id, ct))
            throw new InvalidOperationException("شناسه تکراری است");
        if (!await categoryRepository.ExistsAsync(payload.CategoryId, ct))
            throw new InvalidOperationException("دسته‌بندی یافت نشد");

        var now = DateTimeOffset.UtcNow;
        var story = MapToEntity(payload, id, now);
        await repository.AddAsync(story, ct);
        await SetAudienceAsync(story, payload.Access, ct);
        await auditService.LogAsync("create", "story", story.Id, story.TitleFa, ct: ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<StoryDetailDto> UpdateAsync(string id, StoryPayloadDto payload, CancellationToken ct = default)
    {
        var story = await repository.GetByIdAsync(id, ct: ct)
            ?? throw new KeyNotFoundException("قصه یافت نشد");

        if (!await categoryRepository.ExistsAsync(payload.CategoryId, ct))
            throw new InvalidOperationException("دسته‌بندی یافت نشد");

        var wasPublished = story.Published;
        ApplyPayload(story, payload);
        story.UpdatedAt = DateTimeOffset.UtcNow;
        await SetAudienceAsync(story, payload.Access, ct);
        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync("update", "story", story.Id, story.TitleFa, ct: ct);

        if (wasPublished || story.Published)
            await catalogService.BumpVersionAsync(ct);

        return EntityMappers.ToStoryDetailDto(story);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var story = await repository.GetByIdAsync(id, ct: ct)
            ?? throw new KeyNotFoundException("قصه یافت نشد");

        var wasPublished = story.Published;
        story.DeletedAt = DateTimeOffset.UtcNow;
        story.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync("delete", "story", story.Id, story.TitleFa, ct: ct);

        if (wasPublished)
            await catalogService.BumpVersionAsync(ct);
    }

    public async Task<StoryDto> PublishAsync(string id, bool published, CancellationToken ct = default)
    {
        var story = await repository.GetByIdAsync(id, ct: ct)
            ?? throw new KeyNotFoundException("قصه یافت نشد");

        story.Published = published;
        story.PublishedAt = published ? DateTimeOffset.UtcNow : story.PublishedAt;
        story.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync(published ? "publish" : "unpublish", "story", story.Id, story.TitleFa, ct: ct);
        await catalogService.BumpVersionAsync(ct);
        return EntityMappers.ToStoryDto(story);
    }

    public async Task<StoryDto> ToggleFeaturedAsync(string id, bool featured, CancellationToken ct = default)
    {
        var story = await repository.GetByIdAsync(id, ct: ct)
            ?? throw new KeyNotFoundException("قصه یافت نشد");

        story.Featured = featured;
        story.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync("update", "story", story.Id, story.TitleFa, details: $"featured={featured}", ct: ct);
        return EntityMappers.ToStoryDto(story);
    }

    public async Task<StoryDetailDto> UpdateChaptersAsync(string id, List<StoryChapterDto> chapters, CancellationToken ct = default)
    {
        var story = await repository.GetByIdAsync(id, ct: ct)
            ?? throw new KeyNotFoundException("قصه یافت نشد");

        var wasPublished = story.Published;
        db.StoryChapters.RemoveRange(story.Chapters);

        var order = 0;
        foreach (var chapter in chapters)
        {
            story.Chapters.Add(new StoryChapter
            {
                Id = Guid.NewGuid(),
                StoryId = story.Id,
                TitleFa = chapter.Title.Fa,
                TitleEn = chapter.Title.En,
                StartSeconds = chapter.StartSeconds,
                ImageUrl = chapter.ImageUrl,
                SortOrder = order++
            });
        }

        story.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync("update", "story", story.Id, story.TitleFa, details: "chapters", ct: ct);

        if (wasPublished)
            await catalogService.BumpVersionAsync(ct);

        return EntityMappers.ToStoryDetailDto(story);
    }

    public async Task<List<StoryDto>> ReorderAsync(List<string> ids, CancellationToken ct = default)
    {
        var (stories, _) = await repository.QueryAsync(new StoryQuery(Limit: 10000), ct);
        var map = stories.ToDictionary(s => s.Id);

        for (var i = 0; i < ids.Count; i++)
        {
            if (map.TryGetValue(ids[i], out var story))
            {
                story.SortOrder = i + 1;
                story.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync("reorder", "story", "all", "Stories", ct: ct);
        await catalogService.BumpVersionAsync(ct);

        var (items, _) = await repository.QueryAsync(new StoryQuery(Limit: 10000), ct);
        return items.Select(EntityMappers.ToStoryDto).ToList();
    }

    private async Task<string> ResolveMediaUrlAsync(
        IFormFile? file,
        string? existingUrl,
        string mediaType,
        bool required,
        CancellationToken ct)
    {
        if (file is { Length: > 0 })
        {
            try
            {
                var result = await mediaService.UploadAsync(file, mediaType, ct);
                return result.Url;
            }
            catch (MediaStorageException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                throw new MediaStorageException(ex.Message, ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new MediaStorageException(ex.Message, ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(existingUrl) && IsRemoteUrl(existingUrl))
            return existingUrl;

        if (required)
            throw new MediaStorageException(mediaType == "audio" ? "فایل صوت الزامی است" : "فایل کاور الزامی است");

        return existingUrl ?? string.Empty;
    }

    private async Task<List<StoryChapterDto>> ResolveChaptersAsync(
        string chaptersJson,
        IReadOnlyDictionary<int, IFormFile> chapterImages,
        CancellationToken ct)
    {
        var chapters = JsonSerializer.Deserialize<List<ChapterFormDto>>(chaptersJson, JsonOptions) ?? [];
        var result = new List<StoryChapterDto>();

        for (var i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];
            chapterImages.TryGetValue(i, out var imageFile);
            var imageUrl = await ResolveMediaUrlAsync(
                imageFile,
                chapter.ImageUrl,
                "cover",
                required: true,
                ct);

            result.Add(new StoryChapterDto(
                new LocalizedTextDto(chapter.Title?.Fa ?? string.Empty, chapter.Title?.En ?? string.Empty),
                chapter.StartSeconds,
                imageUrl));
        }

        return result;
    }

    private static StoryAccessDto ParseAccess(string accessJson)
    {
        var access = JsonSerializer.Deserialize<StoryAccessDto>(accessJson, JsonOptions);
        if (access is null)
            throw new MediaStorageException("اطلاعات دسترسی نامعتبر است");

        return access;
    }

    private static bool IsRemoteUrl(string url) =>
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

    private static Story MapToEntity(StoryPayloadDto payload, string id, DateTimeOffset now) => new()
    {
        Id = id,
        CategoryId = payload.CategoryId,
        TitleFa = payload.Title.Fa,
        TitleEn = payload.Title.En,
        DescriptionFa = payload.Description.Fa,
        DescriptionEn = payload.Description.En,
        CoverUrl = payload.CoverUrl,
        AudioUrl = payload.AudioUrl,
        DurationSeconds = payload.DurationSeconds,
        AgeMin = payload.AgeMin,
        AgeMax = payload.AgeMax,
        Featured = payload.Featured,
        SortOrder = payload.SortOrder,
        Published = payload.Published,
        PublishedAt = payload.PublishedAt,
        Visibility = payload.Access.Visibility,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static void ApplyPayload(Story story, StoryPayloadDto payload)
    {
        story.CategoryId = payload.CategoryId;
        story.TitleFa = payload.Title.Fa;
        story.TitleEn = payload.Title.En;
        story.DescriptionFa = payload.Description.Fa;
        story.DescriptionEn = payload.Description.En;
        story.CoverUrl = payload.CoverUrl;
        story.AudioUrl = payload.AudioUrl;
        story.DurationSeconds = payload.DurationSeconds;
        story.AgeMin = payload.AgeMin;
        story.AgeMax = payload.AgeMax;
        story.Featured = payload.Featured;
        story.SortOrder = payload.SortOrder;
        story.Published = payload.Published;
        story.PublishedAt = payload.PublishedAt;
        story.Visibility = payload.Access.Visibility;
    }

    private async Task SetAudienceAsync(Story story, StoryAccessDto access, CancellationToken ct)
    {
        var existingSegments = await db.StoryAudienceSegments
            .Where(x => x.StoryId == story.Id).ToListAsync(ct);
        db.StoryAudienceSegments.RemoveRange(existingSegments);

        var existingUsers = await db.StoryAudienceUsers
            .Where(x => x.StoryId == story.Id).ToListAsync(ct);
        db.StoryAudienceUsers.RemoveRange(existingUsers);

        foreach (var segmentId in access.Audience.SegmentIds.Distinct())
        {
            db.StoryAudienceSegments.Add(new StoryAudienceSegment
            {
                StoryId = story.Id,
                SegmentId = segmentId
            });
        }

        foreach (var userId in access.Audience.UserIds.Distinct())
        {
            db.StoryAudienceUsers.Add(new StoryAudienceUser
            {
                StoryId = story.Id,
                UserId = userId
            });
        }

        await db.SaveChangesAsync(ct);

        if (story.Published)
            await catalogService.BumpVersionAsync(ct);
    }

    private sealed class ChapterFormDto
    {
        public LocalizedTextDto? Title { get; set; }
        public int StartSeconds { get; set; }
        public string? ImageUrl { get; set; }
    }
}
