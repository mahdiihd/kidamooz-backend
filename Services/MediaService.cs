using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Mapping;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IMediaService
{
    UploadUrlResponseDto CreateUploadUrl(UploadUrlRequestDto request);
    Task<ConfirmUploadResponseDto> UploadAsync(IFormFile file, string mediaType, CancellationToken ct = default);
    Task<ConfirmUploadResponseDto> ConfirmUploadAsync(ConfirmUploadRequestDto request, CancellationToken ct = default);
}

public class MediaService(IMediaStorageService storage) : IMediaService
{
    public UploadUrlResponseDto CreateUploadUrl(UploadUrlRequestDto request) =>
        storage.CreateUploadUrl(request.FileName, request.ContentType, request.MediaType);

    public async Task<ConfirmUploadResponseDto> UploadAsync(IFormFile file, string mediaType, CancellationToken ct = default)
    {
        if (file.Length <= 0)
            throw new ArgumentException("فایل خالی است");

        await using var stream = file.OpenReadStream();
        var publicUrl = await storage.UploadAsync(stream, file.FileName, file.ContentType, mediaType, ct);
        return new ConfirmUploadResponseDto(publicUrl);
    }

    public async Task<ConfirmUploadResponseDto> ConfirmUploadAsync(ConfirmUploadRequestDto request, CancellationToken ct = default)
    {
        if (!storage.ValidatePublicUrl(request.PublicUrl, request.MediaType))
            throw new ArgumentException("آدرس فایل معتبر نیست");

        if (!await storage.ObjectExistsAsync(request.PublicUrl, ct))
            throw new ArgumentException("فایل در فضای ذخیره‌سازی یافت نشد");

        return new ConfirmUploadResponseDto(request.PublicUrl);
    }
}

public interface IAudienceService
{
    Task<List<AudienceSegmentDto>> GetSegmentsAsync(CancellationToken ct = default);
    Task<List<AudienceUserDto>> GetUsersAsync(string? search, CancellationToken ct = default);
}

public class AudienceService(IAudienceRepository repository) : IAudienceService
{
    public async Task<List<AudienceSegmentDto>> GetSegmentsAsync(CancellationToken ct = default)
    {
        var segments = await repository.GetSegmentsAsync(ct);
        return segments.Select(EntityMappers.ToAudienceSegmentDto).ToList();
    }

    public async Task<List<AudienceUserDto>> GetUsersAsync(string? search, CancellationToken ct = default)
    {
        var users = await repository.GetUsersAsync(search, ct);
        return users.Select(EntityMappers.ToAudienceUserDto).ToList();
    }
}
