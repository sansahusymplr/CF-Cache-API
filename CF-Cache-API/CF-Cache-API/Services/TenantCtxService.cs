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

    public async Task<string> MintTenantCtxAsync(string tenantId, string entity = "", string clientIp = "", int ttlMinutes = 60)
    {
        var hmacKey = await _secretsService.GetHmacKeyAsync();
        var encryptedIp = EncryptIp(clientIp, hmacKey);

        var payload = new TenantCtx
        {
            tid = tenantId,
            entity = entity,
            ip = encryptedIp,
            exp = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes).ToUnixTimeSeconds(),
            kid = "tenantctx_hmac_key"
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64Url = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

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

    private static string EncryptIp(string ip, byte[] hmacKey)
    {
        if (string.IsNullOrEmpty(ip)) return "";

        // Derive a 256-bit AES key from the HMAC key
        var aesKey = SHA256.HashData(hmacKey);

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(ip);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // IV + ciphertext → base64url
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);
        return Base64UrlEncode(result);
    }
}
