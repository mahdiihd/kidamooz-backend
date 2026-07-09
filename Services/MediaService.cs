using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Mapping;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IMediaService
{
    UploadUrlResponseDto CreateUploadUrl(UploadUrlRequestDto request);
    ConfirmUploadResponseDto ConfirmUpload(ConfirmUploadRequestDto request);
}

public class MediaService(IMediaStorageService storage) : IMediaService
{
    public UploadUrlResponseDto CreateUploadUrl(UploadUrlRequestDto request) =>
        storage.CreateUploadUrl(request.FileName, request.ContentType, request.MediaType);

    public ConfirmUploadResponseDto ConfirmUpload(ConfirmUploadRequestDto request)
    {
        if (!storage.ValidatePublicUrl(request.PublicUrl, request.MediaType))
            throw new ArgumentException("آدرس فایل معتبر نیست");

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
