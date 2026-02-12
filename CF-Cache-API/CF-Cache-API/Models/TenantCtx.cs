namespace CF_Cache_API.Models;

public class TenantCtx
{
    public string tid { get; set; } = string.Empty;
    public long exp { get; set; }
    public string kid { get; set; } = "k1";
}
