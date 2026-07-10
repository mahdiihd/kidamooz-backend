using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/media")]
[Authorize]
public class MediaController(IMediaService mediaService) : ControllerBase
{
    [HttpPost("upload-url")]
    public ActionResult<UploadUrlResponseDto> UploadUrl([FromBody] UploadUrlRequestDto request)
    {
        try
        {
            return Ok(mediaService.CreateUploadUrl(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult<ConfirmUploadResponseDto>> Upload(
        IFormFile file,
        [FromForm] string mediaType,
        CancellationToken ct)
    {
        try
        {
            if (file is null)
                return BadRequest(new { message = "فایل الزامی است" });

            return Ok(await mediaService.UploadAsync(file, mediaType, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<ConfirmUploadResponseDto>> Confirm(
        [FromBody] ConfirmUploadRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await mediaService.ConfirmUploadAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
