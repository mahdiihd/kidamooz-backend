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

    [HttpPost("confirm")]
    public ActionResult<ConfirmUploadResponseDto> Confirm([FromBody] ConfirmUploadRequestDto request)
    {
        try
        {
            return Ok(mediaService.ConfirmUpload(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
