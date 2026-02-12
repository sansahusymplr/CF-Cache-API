using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace CF_Cache_API.Services;

public class SecretsService
{
    private byte[]? _hmacKey;
    private readonly IAmazonSecretsManager? _secretsManager;
    private readonly IConfiguration _configuration;
    private readonly bool _isProduction;

    public SecretsService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _isProduction = environment.IsProduction();
        
        if (_isProduction)
        {
            _secretsManager = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.USEast2);
        }
    }

    public async Task<byte[]> GetHmacKeyAsync()
    {
        if (_hmacKey != null)
            return _hmacKey;

        if (_isProduction && _secretsManager != null)
        {
            var request = new GetSecretValueRequest
            {
                SecretId = "pdm-poc-payer-migration"
            };

            var response = await _secretsManager.GetSecretValueAsync(request);
            var secretJson = JsonDocument.Parse(response.SecretString);
            var base64Key = secretJson.RootElement.GetProperty("tenantctx_hmac_key").GetString();
            
            _hmacKey = Convert.FromBase64String(base64Key!);
        }
        else
        {
            // Development fallback: use a dummy key
            var dummyKey = _configuration["DevelopmentHmacKey"] ?? "dGVzdC1obWFjLWtleS1mb3ItZGV2ZWxvcG1lbnQtb25seQ==";
            _hmacKey = Convert.FromBase64String(dummyKey);
        }
        
        return _hmacKey;
    }
}
