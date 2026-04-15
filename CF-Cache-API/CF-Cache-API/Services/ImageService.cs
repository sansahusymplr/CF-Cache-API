using CF_Cache_API.Models;

namespace CF_Cache_API.Services;

public class ImageService
{
    private readonly Dictionary<string, Image> _images = new();

    public Image UploadImage(string tenantId, string fileName, string contentType, byte[] data)
    {
        var image = new Image
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            ContentType = contentType,
            Data = data,
            Size = data.Length,
            UploadedAt = DateTime.UtcNow,
            TenantId = tenantId
        };

        _images[image.Id] = image;
        return image;
    }

    public Image? GetImage(string tenantId, string id)
    {
        if (_images.TryGetValue(id, out var image) && image.TenantId == tenantId)
            return image;
        return null;
    }

    public IEnumerable<Image> GetAllImages(string tenantId)
    {
        return _images.Values.Where(i => i.TenantId == tenantId);
    }

    public bool DeleteImage(string tenantId, string id)
    {
        if (_images.TryGetValue(id, out var image) && image.TenantId == tenantId)
            return _images.Remove(id);
        return false;
    }
}
