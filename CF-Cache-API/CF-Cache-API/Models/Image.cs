namespace CF_Cache_API.Models;

public class Image
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
