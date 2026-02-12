using CF_Cache_API.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CF_Cache_API.Services;

public class TenantCtxService
{
    private readonly SecretsService _secretsService;

    public TenantCtxService(SecretsService secretsService)
    {
        _secretsService = secretsService;
    }

    public async Task<string> MintTenantCtxAsync(string tenantId, int ttlMinutes = 60)
    {
        var payload = new TenantCtx
        {
            tid = tenantId,
            exp = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes).ToUnixTimeSeconds(),
            kid = "k1"
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64Url = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        var hmacKey = await _secretsService.GetHmacKeyAsync();
        var signature = ComputeHmacSha256(hmacKey, payloadBase64Url);
        var signatureBase64Url = Base64UrlEncode(signature);

        return $"{payloadBase64Url}.{signatureBase64Url}";
    }

    private static byte[] ComputeHmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
