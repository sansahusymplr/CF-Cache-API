using CF_Cache_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CF_Cache_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly ImageService _imageService;

    public ImageController(ImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost("upsert/upload")]
    public async Task<IActionResult> Upload([FromHeader(Name = "X-Tenant-Id")] string tenantId, IFormFile file)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();

        var image = _imageService.UploadImage(tenantId, file.FileName, file.ContentType, data);

        return Ok(new
        {
            message = "Image uploaded successfully",
            data = new
            {
                id = image.Id,
                fileName = image.FileName,
                contentType = image.ContentType,
                size = image.Size,
                uploadedAt = image.UploadedAt
            }
        });
    }

    [HttpGet("search/{tenantId}")]
    public IActionResult GetAll([FromRoute] string tenantId, [FromHeader(Name = "X-Tenant-Id")] string headerTenantId)
    {
        if (string.IsNullOrEmpty(headerTenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });
        if (tenantId != headerTenantId)
            return BadRequest(new { message = "Path tenantId must match X-Tenant-Id header" });

        var images = _imageService.GetAllImages(tenantId).Select(i => new
        {
            id = i.Id,
            fileName = i.FileName,
            contentType = i.ContentType,
            size = i.Size,
            uploadedAt = i.UploadedAt
        });

        return Ok(new { data = images });
    }

    [HttpGet("search/{tenantId}/{id}")]
    public IActionResult GetById([FromRoute] string tenantId, [FromHeader(Name = "X-Tenant-Id")] string headerTenantId, [FromRoute] string id)
    {
        if (string.IsNullOrEmpty(headerTenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });
        if (tenantId != headerTenantId)
            return BadRequest(new { message = "Path tenantId must match X-Tenant-Id header" });

        var image = _imageService.GetImage(tenantId, id);
        if (image == null)
            return NotFound(new { message = "Image not found" });

        return File(image.Data, image.ContentType, image.FileName);
    }

    [HttpDelete("upsert/{id}")]
    public IActionResult Delete([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromRoute] string id)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var deleted = _imageService.DeleteImage(tenantId, id);
        if (!deleted)
            return NotFound(new { message = "Image not found" });

        return Ok(new { message = "Image deleted successfully" });
    }
}
