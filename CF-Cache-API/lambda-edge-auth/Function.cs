using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaEdgeAuth
{
    public class Function
    {
    private static byte[] _cachedSecret;
    private static DateTime _cachedAt = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private const string SecretId = "pdm-poc-payer-migration";
    private const string SecretKeyName = "tenantctx_hmac_key";
    private const string SecretRegion = "us-east-2";

    public async Task<CloudFrontResponse> FunctionHandler(CloudFrontEvent cfEvent, ILambdaContext context)
    {
        if (cfEvent?.Records == null || cfEvent.Records.Count == 0)
            return Deny(400, "Invalid Event");

        var request = cfEvent.Records[0].Cf.Request;

        if (!request.Headers.ContainsKey("cookie"))
            return Deny(401, "Missing Cookie");

        var cookieHeader = string.Join("; ", request.Headers["cookie"].Select(h => h.Value));
        var token = ParseCookie(cookieHeader, "TenantCtx");

        if (string.IsNullOrEmpty(token))
            return Deny(401, "Missing TenantCtx");

        var parts = token.Split('.');
        if (parts.Length != 2)
            return Deny(401, "Invalid Format");

        var payloadB64 = parts[0];
        var sigB64 = parts[1];

        var secretBytes = await GetSecretBytes();

        using var hmac = new HMACSHA256(secretBytes);
        var expectedSig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
        var providedSig = Base64UrlDecode(sigB64);

        if (!CryptographicOperations.FixedTimeEquals(expectedSig, providedSig))
            return Deny(401, "Invalid Signature");

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadB64));
        var payload = JsonSerializer.Deserialize<TenantPayload>(payloadJson);

        if (payload == null || payload.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return Deny(401, "Expired");

        // Validate encrypted IP against request's X-Forwarded-For
        if (!string.IsNullOrEmpty(payload.Ip))
        {
            var requestIp = "";
            if (request.Headers.ContainsKey("x-forwarded-for"))
                requestIp = request.Headers["x-forwarded-for"][0].Value.Split(',')[0].Trim();

            var decryptedIp = DecryptIp(payload.Ip, secretBytes);
            if (decryptedIp != requestIp)
                return Deny(401, "IP Mismatch");
        }

        request.Headers["x-tenant-id"] = new List<CloudFrontHeader>
        {
            new CloudFrontHeader
            {
                Key = "X-Tenant-Id",
                Value = payload.Tid
            }
        };

        if (!string.IsNullOrEmpty(payload.Entity))
        {
            request.Headers["x-entity"] = new List<CloudFrontHeader>
            {
                new CloudFrontHeader
                {
                    Key = "X-Entity",
                    Value = payload.Entity
                }
            };
        }

        return null;
    }

    private async Task<byte[]> GetSecretBytes()
    {
        if (_cachedSecret != null && DateTime.UtcNow - _cachedAt < CacheTtl)
            return _cachedSecret;

        var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(SecretRegion));
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = SecretId
        });

        var json = JsonDocument.Parse(response.SecretString);
        var base64 = json.RootElement.GetProperty(SecretKeyName).GetString();

        _cachedSecret = Convert.FromBase64String(base64);
        _cachedAt = DateTime.UtcNow;
        return _cachedSecret;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        return Convert.FromBase64String(input);
    }

    private static string ParseCookie(string cookieHeader, string name)
    {
        foreach (var part in cookieHeader.Split(';'))
        {
            var kv = part.Trim().Split('=');
            if (kv.Length == 2 && kv[0] == name)
                return kv[1];
        }
        return null;
    }

    private CloudFrontResponse Deny(int status, string message)
    {
        return new CloudFrontResponse
        {
            Status = status.ToString(),
            StatusDescription = message,
            Headers = new Dictionary<string, IList<CloudFrontHeader>>
            {
                {
                    "cache-control",
                    new List<CloudFrontHeader>
                    {
                        new CloudFrontHeader { Key = "Cache-Control", Value = "no-store" }
                    }
                }
            }
        };
    }

    private static string DecryptIp(string encryptedIp, byte[] hmacKey)
    {
        try
        {
            var combined = Base64UrlDecode(encryptedIp);

            var nonce = new byte[16];
            var cipherBytes = new byte[combined.Length - 16];
            Array.Copy(combined, 0, nonce, 0, 16);
            Array.Copy(combined, 16, cipherBytes, 0, cipherBytes.Length);

            // Same keystream: HMAC-SHA256(key, nonce)
            using var hmac = new HMACSHA256(hmacKey);
            var keystream = hmac.ComputeHash(nonce);

            // XOR to recover plaintext
            var plain = new byte[cipherBytes.Length];
            for (int i = 0; i < cipherBytes.Length; i++)
                plain[i] = (byte)(cipherBytes[i] ^ keystream[i % keystream.Length]);

            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return "";
        }
    }

        private class TenantPayload
        {
            public string Tid { get; set; }
            public string Entity { get; set; }
            public string Ip { get; set; }
            public long Exp { get; set; }
        }
    }
}
