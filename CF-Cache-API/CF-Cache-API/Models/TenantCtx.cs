namespace CF_Cache_API.Models;

public class TenantCtx
{
    public string tid { get; set; } = string.Empty;
    public string entity { get; set; } = string.Empty;
    public long exp { get; set; }
    public string ip { get; set; } = string.Empty;
    public string kid { get; set; } = "k1";
}
