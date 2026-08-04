using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure;
using Kidamooz.Infrastructure.Ai;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Infrastructure.Security;
using Kidamooz.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kidamooz.Services;

public interface IStoryDraftService
{
    Task<StoryDraftDto> CreateFromDrawingAsync(string userId, string deviceId, IFormFile drawing, CancellationToken ct = default);
    Task<StoryDraftQuotaDto> GetQuotaAsync(string userId, CancellationToken ct = default);
    Task<List<StoryDraftDto>> ListAsync(string userId, CancellationToken ct = default);
    Task<StoryDraftDto> GetAsync(string userId, Guid id, CancellationToken ct = default);
    Task<StoryDraftDto> UpdateAsync(string userId, Guid id, UpdateStoryDraftRequestDto request, CancellationToken ct = default);
    Task<StoryDraftDto> RewriteAsync(string userId, Guid id, RewriteStoryDraftRequestDto request, CancellationToken ct = default);
    Task<StoryDraftDto> RegenerateCoverAsync(string userId, Guid id, RegenerateCoverRequestDto? request, CancellationToken ct = default);
    Task<StoryDraftDto> UploadAudioAsync(string userId, Guid id, IFormFile audio, int? durationSeconds, CancellationToken ct = default);
    Task<StoryDraftDto> SubmitForReviewAsync(string userId, Guid id, CancellationToken ct = default);
    Task<List<StoryDraftDto>> AdminListPendingAsync(CancellationToken ct = default);
    Task<List<StoryDraftDto>> AdminListAsync(string? status, CancellationToken ct = default);
    Task<StoryDraftDto> AdminGetAsync(Guid id, CancellationToken ct = default);
    Task<ApproveStoryDraftResponseDto> AdminApproveAsync(Guid id, CancellationToken ct = default);
    Task<StoryDraftDto> AdminRejectAsync(Guid id, string? reason, CancellationToken ct = default);
    Task RemoveFromProfileAsync(string userId, Guid id, CancellationToken ct = default);
    Task MarkDraftsDeletedForStoryAsync(string storyId, CancellationToken ct = default);
}

public class StoryDraftService(
    AppDbContext db,
    IMediaStorageService storage,
    IMediaUrlNormalizer mediaUrls,
    IGeminiStoryClient gemini,
    ICoverImageGenerator coverGenerator,
    IAudioNarrationService narration,
    IOptions<NarrationSettings> narrationOptions,
    IMemberEngagementService engagement,
    ICatalogService catalogService,
    ILogger<StoryDraftService> logger) : IStoryDraftService
{
    public const string PersonalCategoryId = "personal";
    public const string StorytellingCategoryId = "wonder";
    public const int FreeDailyCreateLimit = 1;
    public const int PlusDailyCreateLimit = 5;

    private static readonly HashSet<string> UnlimitedMobiles = new(StringComparer.Ordinal)
    {
        "09196079395"
    };

    private static readonly HashSet<string> AllowedUploadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".m4a",
        ".webm",
        ".ogg"
    };

    public async Task<StoryDraftDto> CreateFromDrawingAsync(
        string userId,
        string deviceId,
        IFormFile drawing,
        CancellationToken ct = default)
    {
        EnsureUser(userId);
        if (drawing is null || drawing.Length <= 0)
            throw new ArgumentException("تصویر نقاشی الزامی است.");

        await EnsureDailyCreateAllowedAsync(userId, ct);

        var user = await db.AppUsers.AsNoTracking().FirstAsync(x => x.Id == userId, ct);
        var draft = new StoryDraft
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? "unknown" : deviceId.Trim(),
            Status = StoryDraftStatuses.Generating,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.StoryDrafts.Add(draft);
        await db.SaveChangesAsync(ct);

        try
        {
            await using var buffer = new MemoryStream();
            await drawing.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();

            await using (var drawingStream = new MemoryStream(bytes))
            {
                draft.DrawingUrl = await storage.UploadAsync(
                    drawingStream,
                    drawing.FileName,
                    drawing.ContentType ?? "image/jpeg",
                    "drawing",
                    ct);
            }

            GeneratedStoryContent content;
            await using (var visionStream = new MemoryStream(bytes))
            {
                content = await gemini.GenerateFromDrawingAsync(
                    visionStream,
                    drawing.ContentType ?? "image/jpeg",
                    drawing.FileName,
                    ct);
            }

            ApplyGeneratedContent(draft, content);

            var coverTask = GenerateCoverBytesAsync(content.CoverPrompt, ct);
            var audioTask = GenerateNarrationSafelyAsync(draft.Id, content.StoryScript, ct);
            await Task.WhenAll(coverTask, audioTask);

            var coverBytes = await coverTask;
            if (coverBytes is { Length: > 0 })
            {
                await using var coverStream = new MemoryStream(coverBytes);
                draft.CoverUrl = await storage.UploadAsync(
                    coverStream,
                    $"{draft.Id:N}.jpg",
                    "image/jpeg",
                    "cover",
                    ct);
                draft.UsedFallbackCover = false;
            }
            else
            {
                draft.CoverUrl = draft.DrawingUrl;
                draft.UsedFallbackCover = true;
                logger.LogWarning("Using drawing as fallback cover for draft {DraftId}", draft.Id);
            }

            draft.AudioUrl = await audioTask;
            draft.Status = StoryDraftStatuses.Ready;
            draft.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            await engagement.RecordCreateActivityAsync(userId, ct);
            return ToDto(draft, user);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            draft.Status = StoryDraftStatuses.Failed;
            draft.ErrorMessage = PlainTextSanitizer.Clean(ex.Message, 500);
            draft.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    public async Task<StoryDraftQuotaDto> GetQuotaAsync(string userId, CancellationToken ct = default)
    {
        EnsureUser(userId);
        await FailStaleGeneratingAsync(userId, ct);

        var user = await db.AppUsers.AsNoTracking().FirstAsync(x => x.Id == userId, ct);
        var isPlus = MemberEngagementService.IsPlusActive(user);
        var dailyLimit = isPlus ? PlusDailyCreateLimit : FreeDailyCreateLimit;
        var planTier = isPlus ? MemberPlans.Plus : MemberPlans.Free;

        if (await IsUnlimitedUserAsync(userId, ct))
        {
            return new StoryDraftQuotaDto(true, dailyLimit, 0, null, planTier, isPlus);
        }

        var usedToday = await CountTodayCreatesAsync(userId, ct);
        var canCreate = usedToday < dailyLimit;
        return new StoryDraftQuotaDto(
            canCreate,
            dailyLimit,
            usedToday,
            canCreate ? null : TehranTime.StartOfTomorrowUtc(),
            planTier,
            isPlus);
    }

    public async Task<List<StoryDraftDto>> ListAsync(string userId, CancellationToken ct = default)
    {
        EnsureUser(userId);
        await FailStaleGeneratingAsync(userId, ct);
        await SyncDeletedPublishedDraftsAsync(userId, ct);
        var items = await db.StoryDrafts.AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.UserId == userId
                        && x.Status != StoryDraftStatuses.Failed
                        && x.Status != StoryDraftStatuses.Generating
                        && x.Status != StoryDraftStatuses.Archived)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(50)
            .ToListAsync(ct);
        return items.Select(d => ToDto(d, d.User)).ToList();
    }

    public async Task<StoryDraftDto> GetAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var draft = await GetOwnedAsync(userId, id, ct);
        return ToDto(draft, draft.User);
    }

    public async Task<StoryDraftDto> UpdateAsync(
        string userId,
        Guid id,
        UpdateStoryDraftRequestDto request,
        CancellationToken ct = default)
    {
        var draft = await GetOwnedAsync(userId, id, ct);
        if (draft.Status is StoryDraftStatuses.Generating or StoryDraftStatuses.Failed or StoryDraftStatuses.PendingReview or StoryDraftStatuses.Published)
            throw new InvalidOperationException("این پیش‌نویس قابل ویرایش نیست.");

        if (!string.IsNullOrWhiteSpace(request.TitleFa))
            draft.TitleFa = PlainTextSanitizer.Clean(request.TitleFa, 300);
        if (!string.IsNullOrWhiteSpace(request.DescriptionFa))
            draft.DescriptionFa = PlainTextSanitizer.Clean(request.DescriptionFa, 2000);
        if (!string.IsNullOrWhiteSpace(request.TitleEn))
            draft.TitleEn = PlainTextSanitizer.Clean(request.TitleEn, 300);
        if (!string.IsNullOrWhiteSpace(request.DescriptionEn))
            draft.DescriptionEn = PlainTextSanitizer.Clean(request.DescriptionEn, 2000);
        if (!string.IsNullOrWhiteSpace(request.StoryScript))
            draft.StoryScript = PlainTextSanitizer.Clean(request.StoryScript, 8000);
        if (request.ChallengeTag is not null)
            draft.ChallengeTag = string.IsNullOrWhiteSpace(request.ChallengeTag)
                ? null
                : PlainTextSanitizer.Clean(request.ChallengeTag, 64);

        draft.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(draft, draft.User);
    }

    public async Task<StoryDraftDto> RewriteAsync(
        string userId,
        Guid id,
        RewriteStoryDraftRequestDto request,
        CancellationToken ct = default)
    {
        var draft = await GetOwnedAsync(userId, id, ct);
        if (draft.Status is StoryDraftStatuses.Generating or StoryDraftStatuses.Failed or StoryDraftStatuses.PendingReview or StoryDraftStatuses.Published)
            throw new InvalidOperationException("این پیش‌نویس قابل بازنویسی نیست.");

        var mode = string.IsNullOrWhiteSpace(request.Mode) ? "polish" : request.Mode.Trim();
        var content = await gemini.RewriteAsync(
            draft.TitleFa,
            draft.DescriptionFa,
            draft.StoryScript,
            mode,
            ct);
        ApplyGeneratedContent(draft, content);
        draft.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(draft, draft.User);
    }

    public async Task<StoryDraftDto> RegenerateCoverAsync(
        string userId,
        Guid id,
        RegenerateCoverRequestDto? request,
        CancellationToken ct = default)
    {
        var draft = await GetOwnedAsync(userId, id, ct);
        if (draft.Status is StoryDraftStatuses.Generating or StoryDraftStatuses.Failed or StoryDraftStatuses.PendingReview or StoryDraftStatuses.Published)
            throw new InvalidOperationException("کاور این پیش‌نویس قابل تغییر نیست.");

        var prompt = !string.IsNullOrWhiteSpace(request?.PromptHint)
            ? PlainTextSanitizer.Clean(request!.PromptHint!, 1000)
            : draft.CoverPrompt;
        if (string.IsNullOrWhiteSpace(prompt))
        {
            prompt =
                $"Children's book illustration for Persian kids story titled {draft.TitleFa}, colorful, joyful, no text on image";
        }

        draft.CoverPrompt = prompt;
        var coverBytes = await coverGenerator.GenerateAsync(prompt, ct);
        if (coverBytes is { Length: > 0 })
        {
            await using var coverStream = new MemoryStream(coverBytes);
            draft.CoverUrl = await storage.UploadAsync(
                coverStream,
                $"{draft.Id:N}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.jpg",
                "image/jpeg",
                "cover",
                ct);
            draft.UsedFallbackCover = false;
        }
        else if (!string.IsNullOrWhiteSpace(draft.DrawingUrl))
        {
            draft.CoverUrl = draft.DrawingUrl;
            draft.UsedFallbackCover = true;
        }
        else
        {
            throw new InvalidOperationException("تولید کاور جدید ناموفق بود.");
        }

        draft.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(draft, draft.User);
    }

    public async Task<StoryDraftDto> UploadAudioAsync(
        string userId,
        Guid id,
        IFormFile audio,
        int? durationSeconds,
        CancellationToken ct = default)
    {
        var draft = await GetOwnedAsync(userId, id, ct);
        if (draft.Status is not (StoryDraftStatuses.Ready or StoryDraftStatuses.AudioUploaded or StoryDraftStatuses.Rejected))
            throw new InvalidOperationException("ابتدا باید متن قصه آماده باشد.");
        if (audio is null || audio.Length <= 0)
            throw new ArgumentException("فایل صدا الزامی است.");

        var maxUploadSize = narrationOptions.Value.MaxUploadSize;
        if (audio.Length > maxUploadSize)
            throw new ArgumentException("حجم فایل صدا نباید بیشتر از ۵۰ مگابایت باشد.");

        var extension = Path.GetExtension(audio.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedUploadExtensions.Contains(extension))
            throw new ArgumentException("فقط فایل‌های صوتی mp3، wav، m4a، webm و ogg مجاز هستند.");

        await using var stream = audio.OpenReadStream();
        draft.UploadedAudioUrl = await storage.UploadAsync(
            stream,
            audio.FileName,
            audio.ContentType ?? ResolveUploadContentType(extension),
            "user-audio",
            ct);
        draft.DurationSeconds = durationSeconds is > 0 ? durationSeconds : draft.DurationSeconds;
        draft.Status = StoryDraftStatuses.AudioUploaded;
        draft.RejectReason = null;
        draft.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(draft, draft.User);
    }

    public async Task<StoryDraftDto> SubmitForReviewAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var draft = await GetOwnedAsync(userId, id, ct);
        if (string.IsNullOrWhiteSpace(draft.AudioUrl) && string.IsNullOrWhiteSpace(draft.UploadedAudioUrl))
            throw new InvalidOperationException("ابتدا صدای قصه را تولید یا بارگذاری کنید.");
        if (string.IsNullOrWhiteSpace(draft.CoverUrl))
            throw new InvalidOperationException("کاور قصه آماده نیست.");
        if (draft.Status is StoryDraftStatuses.PendingReview or StoryDraftStatuses.Published)
            throw new InvalidOperationException("این قصه قبلاً ارسال شده است.");

        draft.DurationSeconds = ResolveDurationSeconds(draft);
        draft.Status = StoryDraftStatuses.PendingReview;
        draft.SubmittedAt = DateTimeOffset.UtcNow;
        draft.RejectReason = null;
        draft.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(draft, draft.User);
    }

    public async Task<List<StoryDraftDto>> AdminListPendingAsync(CancellationToken ct = default)
    {
        var items = await db.StoryDrafts.AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.Status == StoryDraftStatuses.PendingReview)
            .OrderBy(x => x.SubmittedAt)
            .Take(100)
            .ToListAsync(ct);
        return items.Select(d => ToDto(d, d.User)).ToList();
    }

    public async Task<List<StoryDraftDto>> AdminListAsync(string? status, CancellationToken ct = default)
    {
        await SyncDeletedPublishedDraftsAsync(userId: null, ct);
        var query = db.StoryDrafts.AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.Status != StoryDraftStatuses.Generating
                        && x.Status != StoryDraftStatuses.Failed
                        && x.Status != StoryDraftStatuses.Archived);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status == normalized);
        }

        var items = await query
            .OrderByDescending(x => x.UpdatedAt)
            .Take(200)
            .ToListAsync(ct);
        return items.Select(d => ToDto(d, d.User)).ToList();
    }

    public async Task RemoveFromProfileAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var draft = await GetOwnedAsync(userId, id, ct);
        if (draft.Status == StoryDraftStatuses.Published)
            throw new InvalidOperationException("قصه منتشرشده را نمی‌توانی از پروفایل حذف کنی.");
        if (draft.Status == StoryDraftStatuses.PendingReview)
            throw new InvalidOperationException("قصه در صف بررسی است و فعلاً قابل حذف از پروفایل نیست.");

        draft.Status = StoryDraftStatuses.Archived;
        draft.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkDraftsDeletedForStoryAsync(string storyId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storyId))
            return;

        var drafts = await db.StoryDrafts
            .Where(x => x.PublishedStoryId == storyId && x.Status == StoryDraftStatuses.Published)
            .ToListAsync(ct);
        if (drafts.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var draft in drafts)
        {
            draft.Status = StoryDraftStatuses.Deleted;
            draft.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<StoryDraftDto> AdminGetAsync(Guid id, CancellationToken ct = default)
    {
        var draft = await db.StoryDrafts.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("پیش‌نویس یافت نشد.");
        return ToDto(draft, draft.User);
    }

    public async Task<ApproveStoryDraftResponseDto> AdminApproveAsync(Guid id, CancellationToken ct = default)
    {
        var draft = await db.StoryDrafts.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("پیش‌نویس یافت نشد.");

        var canApprove = draft.Status is StoryDraftStatuses.PendingReview or StoryDraftStatuses.Deleted;
        if (!canApprove)
            throw new InvalidOperationException("این پیش‌نویس قابل تأیید یا بازانتشار نیست.");
        if ((string.IsNullOrWhiteSpace(draft.AudioUrl) && string.IsNullOrWhiteSpace(draft.UploadedAudioUrl))
            || string.IsNullOrWhiteSpace(draft.CoverUrl))
            throw new InvalidOperationException("فایل‌های قصه ناقص است.");

        var categoryId = await ResolveStorytellingCategoryIdAsync(ct);

        var authorName = PlainTextSanitizer.Clean(
            string.IsNullOrWhiteSpace(draft.User?.DisplayName)
                ? draft.User?.Mobile ?? "کاربر"
                : draft.User.DisplayName,
            200);

        var titleFa = PlainTextSanitizer.Clean(draft.TitleFa, 300);
        var descriptionFa = PlainTextSanitizer.Clean(draft.DescriptionFa, 2000);
        var titleEn = PlainTextSanitizer.Clean(
            string.IsNullOrWhiteSpace(draft.TitleEn) ? titleFa : draft.TitleEn,
            300);
        var descriptionEn = PlainTextSanitizer.Clean(
            string.IsNullOrWhiteSpace(draft.DescriptionEn) ? descriptionFa : draft.DescriptionEn,
            2000);
        var coverUrl = mediaUrls.Normalize(draft.CoverUrl);
        var audioUrl = mediaUrls.Normalize(draft.AudioUrl);
        var uploadedAudioUrl = string.IsNullOrWhiteSpace(draft.UploadedAudioUrl)
            ? null
            : mediaUrls.Normalize(draft.UploadedAudioUrl);
        if (string.IsNullOrWhiteSpace(coverUrl))
            throw new InvalidOperationException("آدرس فایل‌های قصه نامعتبر است.");
        if (string.IsNullOrWhiteSpace(audioUrl) && string.IsNullOrWhiteSpace(uploadedAudioUrl))
            throw new InvalidOperationException("آدرس فایل‌های قصه نامعتبر است.");
        if (string.IsNullOrWhiteSpace(audioUrl))
            audioUrl = uploadedAudioUrl!;

        var durationSeconds = ResolveDurationSeconds(draft);
        var now = DateTimeOffset.UtcNow;
        Story story;
        if (!string.IsNullOrWhiteSpace(draft.PublishedStoryId))
        {
            story = await db.Stories.FirstOrDefaultAsync(x => x.Id == draft.PublishedStoryId, ct)
                ?? throw new InvalidOperationException("قصه منتشرشده قبلی یافت نشد.");
            ApplyStoryFields(
                story,
                categoryId,
                titleFa,
                titleEn,
                descriptionFa,
                descriptionEn,
                coverUrl,
                audioUrl,
                uploadedAudioUrl,
                durationSeconds,
                authorName,
                draft.UserId,
                now);
            story.DeletedAt = null;
            if (story.PublishedAt is null)
                story.PublishedAt = now;
        }
        else
        {
            var storyId = Guid.NewGuid().ToString("N")[..8];
            story = new Story
            {
                Id = storyId,
                ProgressIcon = "star",
                AgeMin = 3,
                AgeMax = 8,
                Featured = false,
                SortOrder = 0,
                CreatedAt = now
            };
            ApplyStoryFields(
                story,
                categoryId,
                titleFa,
                titleEn,
                descriptionFa,
                descriptionEn,
                coverUrl,
                audioUrl,
                uploadedAudioUrl,
                durationSeconds,
                authorName,
                draft.UserId,
                now);
            story.PublishedAt = now;
            db.Stories.Add(story);
            draft.PublishedStoryId = storyId;
        }

        draft.TitleFa = titleFa;
        draft.DescriptionFa = descriptionFa;
        draft.TitleEn = titleEn;
        draft.DescriptionEn = descriptionEn;
        draft.DurationSeconds = durationSeconds;
        draft.StoryScript = PlainTextSanitizer.Clean(draft.StoryScript, 8000);
        draft.Status = StoryDraftStatuses.Published;
        draft.RejectReason = null;
        draft.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        await catalogService.BumpVersionAsync(ct);
        return new ApproveStoryDraftResponseDto(draft.Id, story.Id, ToDto(draft, draft.User));
    }

    private static void ApplyStoryFields(
        Story story,
        string categoryId,
        string titleFa,
        string titleEn,
        string descriptionFa,
        string descriptionEn,
        string coverUrl,
        string audioUrl,
        string? uploadedAudioUrl,
        int durationSeconds,
        string authorName,
        string? authorUserId,
        DateTimeOffset now)
    {
        story.CategoryId = categoryId;
        story.TitleFa = titleFa;
        story.TitleEn = titleEn;
        story.DescriptionFa = descriptionFa;
        story.DescriptionEn = descriptionEn;
        story.CoverUrl = coverUrl;
        story.AudioUrl = audioUrl;
        story.UploadedAudioUrl = uploadedAudioUrl;
        story.DurationSeconds = durationSeconds;
        story.AuthorName = authorName;
        story.AuthorUserId = authorUserId;
        story.Published = true;
        story.Visibility = "public";
        story.UpdatedAt = now;
    }

    private static int ResolveDurationSeconds(StoryDraft draft)
    {
        if (draft.DurationSeconds is > 0)
            return draft.DurationSeconds.Value;

        var words = draft.StoryScript
            .Split([' ', '\n', '\r', '\t', '،', '.', '!', '؟', '?'], StringSplitOptions.RemoveEmptyEntries)
            .Length;
        return Math.Clamp(words * 60 / 140, 45, 600);
    }

    public async Task<StoryDraftDto> AdminRejectAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var draft = await db.StoryDrafts.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("پیش‌نویس یافت نشد.");
        if (draft.Status != StoryDraftStatuses.PendingReview)
            throw new InvalidOperationException("این پیش‌نویس در صف بررسی نیست.");

        draft.Status = StoryDraftStatuses.Rejected;
        draft.RejectReason = PlainTextSanitizer.Clean(
            string.IsNullOrWhiteSpace(reason) ? "رد شد توسط ادمین" : reason,
            500);
        draft.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(draft, draft.User);
    }

    private Task<byte[]?> GenerateCoverBytesAsync(string coverPrompt, CancellationToken ct) =>
        coverGenerator.GenerateAsync(coverPrompt, ct);

    private async Task<string?> GenerateNarrationSafelyAsync(
        Guid storyId,
        string storyText,
        CancellationToken ct)
    {
        try
        {
            var relativeUrl = await narration.GenerateAsync(storyId, storyText, ct);
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return null;

            var localPath = narration.ResolveLocalPath(relativeUrl);
            if (!File.Exists(localPath))
            {
                logger.LogWarning(
                    "Narration file missing after generation for {StoryId}. Path={Path}",
                    storyId,
                    localPath);
                return null;
            }

            await using var stream = File.OpenRead(localPath);
            var publicUrl = await storage.UploadAsync(
                stream,
                $"{storyId:N}.mp3",
                "audio/mpeg",
                "audio",
                ct);
            logger.LogInformation(
                "Narration uploaded for {StoryId}. LocalPath={LocalPath}, PublicUrl={PublicUrl}",
                storyId,
                localPath,
                publicUrl);
            return publicUrl;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Narration generation failed for {StoryId}; continuing without audio", storyId);
            return null;
        }
    }

    private static string ResolveUploadContentType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".webm" => "audio/webm",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };

    private async Task EnsureDailyCreateAllowedAsync(string userId, CancellationToken ct)
    {
        await FailStaleGeneratingAsync(userId, ct);
        if (await IsUnlimitedUserAsync(userId, ct))
            return;

        var user = await db.AppUsers.AsNoTracking().FirstAsync(x => x.Id == userId, ct);
        var dailyLimit = MemberEngagementService.IsPlusActive(user)
            ? PlusDailyCreateLimit
            : FreeDailyCreateLimit;

        var usedToday = await CountTodayCreatesAsync(userId, ct);
        if (usedToday >= dailyLimit)
            throw new DailyStoryLimitException(TehranTime.StartOfTomorrowUtc());
    }

    private async Task<bool> IsUnlimitedUserAsync(string userId, CancellationToken ct)
    {
        var mobile = await db.AppUsers.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.Mobile)
            .FirstOrDefaultAsync(ct);
        var normalized = MobileNormalizer.Normalize(mobile);
        return !string.IsNullOrEmpty(normalized) && UnlimitedMobiles.Contains(normalized);
    }

    private Task<int> CountTodayCreatesAsync(string userId, CancellationToken ct)
    {
        var startUtc = TehranTime.StartOfTodayUtc();
        var endUtc = TehranTime.StartOfTomorrowUtc();
        return db.StoryDrafts.AsNoTracking().CountAsync(
            x => x.UserId == userId
                 && x.CreatedAt >= startUtc
                 && x.CreatedAt < endUtc,
            ct);
    }

    private async Task SyncDeletedPublishedDraftsAsync(string? userId, CancellationToken ct)
    {
        var query = db.StoryDrafts.Where(x => x.Status == StoryDraftStatuses.Published
                                             && x.PublishedStoryId != null);
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(x => x.UserId == userId);

        var drafts = await query.ToListAsync(ct);
        if (drafts.Count == 0)
            return;

        var storyIds = drafts
            .Select(x => x.PublishedStoryId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var liveIds = await db.Stories.AsNoTracking()
            .Where(x => storyIds.Contains(x.Id) && x.DeletedAt == null)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var liveSet = liveIds.ToHashSet(StringComparer.Ordinal);

        var changed = false;
        var now = DateTimeOffset.UtcNow;
        foreach (var draft in drafts)
        {
            if (liveSet.Contains(draft.PublishedStoryId!))
                continue;
            draft.Status = StoryDraftStatuses.Deleted;
            draft.UpdatedAt = now;
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(ct);
    }

    private async Task FailStaleGeneratingAsync(string userId, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-15);
        var stale = await db.StoryDrafts
            .Where(x => x.UserId == userId
                        && x.Status == StoryDraftStatuses.Generating
                        && x.UpdatedAt < cutoff)
            .ToListAsync(ct);
        if (stale.Count == 0)
            return;

        foreach (var draft in stale)
        {
            draft.Status = StoryDraftStatuses.Failed;
            draft.ErrorMessage = PlainTextSanitizer.Clean("ساخت قصه ناتمام ماند.", 500);
            draft.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task EnsurePersonalCategoryAsync(CancellationToken ct)
    {
        var exists = await db.Categories.AnyAsync(x => x.Id == PersonalCategoryId, ct);
        if (exists)
        {
            var cat = await db.Categories.FirstAsync(x => x.Id == PersonalCategoryId, ct);
            if (!cat.Published)
            {
                cat.Published = true;
                cat.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        var now = DateTimeOffset.UtcNow;
        db.Categories.Add(new Category
        {
            Id = PersonalCategoryId,
            Slug = "personal",
            TitleFa = "قصه‌های کاربران",
            TitleEn = "User Stories",
            IconUrl = string.Empty,
            Color = "#7bc950",
            SortOrder = 999,
            Published = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> ResolveStorytellingCategoryIdAsync(CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(
            x => x.DeletedAt == null && (
                x.Id == StorytellingCategoryId
                || x.Slug == "wonder"
                || x.TitleFa == "خیال‌خونه"),
            ct);

        if (category is null)
            throw new InvalidOperationException("دسته «خیال‌خونه» یافت نشد. ابتدا آن را در پنل بسازید.");

        if (!category.Published)
        {
            category.Published = true;
            category.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return category.Id;
    }

    private async Task<StoryDraft> GetOwnedAsync(string userId, Guid id, CancellationToken ct)
    {
        EnsureUser(userId);
        var draft = await db.StoryDrafts.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("پیش‌نویس یافت نشد.");
        if (!string.Equals(draft.UserId, userId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("دسترسی به این پیش‌نویس مجاز نیست.");
        return draft;
    }

    private static void EnsureUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("ورود الزامی است.");
    }

    private static void ApplyGeneratedContent(StoryDraft draft, GeneratedStoryContent content)
    {
        draft.TitleFa = PlainTextSanitizer.Clean(content.TitleFa, 300);
        draft.DescriptionFa = PlainTextSanitizer.Clean(content.DescriptionFa, 2000);
        draft.TitleEn = PlainTextSanitizer.Clean(content.TitleEn, 300);
        draft.DescriptionEn = PlainTextSanitizer.Clean(content.DescriptionEn, 2000);
        draft.StoryScript = PlainTextSanitizer.Clean(content.StoryScript, 8000);
        draft.CoverPrompt = PlainTextSanitizer.Clean(content.CoverPrompt, 1000);
    }

    private StoryDraftDto ToDto(StoryDraft d, AppUser? user) => new(
        d.Id,
        d.Status,
        SafeUrl(d.DrawingUrl),
        SafeUrl(d.CoverUrl),
        d.UsedFallbackCover,
        PlainTextSanitizer.Clean(d.TitleFa, 300),
        PlainTextSanitizer.Clean(d.DescriptionFa, 2000),
        PlainTextSanitizer.Clean(d.TitleEn, 300),
        PlainTextSanitizer.Clean(d.DescriptionEn, 2000),
        PlainTextSanitizer.Clean(d.StoryScript, 8000),
        PlainTextSanitizer.CleanOptional(d.ChallengeTag, 64),
        SafeUrl(d.AudioUrl),
        SafeUrl(d.UploadedAudioUrl),
        d.DurationSeconds,
        d.PublishedStoryId,
        PlainTextSanitizer.CleanOptional(d.ErrorMessage, 500),
        PlainTextSanitizer.CleanOptional(d.RejectReason, 500),
        PlainTextSanitizer.CleanOptional(user?.DisplayName, 200),
        user?.Mobile,
        d.SubmittedAt,
        d.CreatedAt,
        d.UpdatedAt,
        CanRemoveFromProfile(d.Status));

    private static bool CanRemoveFromProfile(string status) =>
        status is not (StoryDraftStatuses.Published or StoryDraftStatuses.PendingReview or StoryDraftStatuses.Generating);

    private string? SafeUrl(string? url)
    {
        var normalized = mediaUrls.Normalize(url);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
