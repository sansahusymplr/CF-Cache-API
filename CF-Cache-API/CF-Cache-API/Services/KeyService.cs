using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;

namespace CF_Cache_API.Services;

public class KeyService
{
    private const string TableName = "poc-payer-migration";
    private readonly AmazonDynamoDBClient _dynamo;
    private readonly AmazonKeyManagementServiceClient _kms;

    public KeyService()
    {
        _dynamo = new AmazonDynamoDBClient(Amazon.RegionEndpoint.USEast2);
        _kms = new AmazonKeyManagementServiceClient(Amazon.RegionEndpoint.USEast2);
    }

    public async Task<(string kid, byte[] key)> GenerateAndStoreKeyAsync(string email)
    {
        // Generate 32-byte random key via KMS
        var kmsResponse = await _kms.GenerateRandomAsync(new GenerateRandomRequest
        {
            NumberOfBytes = 32
        });
        var keyBytes = kmsResponse.Plaintext.ToArray();
        var keyBase64 = Convert.ToBase64String(keyBytes);

        var kid = Guid.NewGuid().ToString();

        // Store in DynamoDB: email + kid + SecretKey
        await _dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["email"] = new AttributeValue { S = email },
                ["kid"] = new AttributeValue { S = kid },
                ["SecretKey"] = new AttributeValue { S = keyBase64 },
                ["createdAt"] = new AttributeValue { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
            }
        });

        return (kid, keyBytes);
    }

    public async Task<byte[]?> GetKeyAsync(string email, string kid)
    {
        var response = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["email"] = new AttributeValue { S = email },
                ["kid"] = new AttributeValue { S = kid }
            }
        });

        if (response.Item == null || !response.Item.ContainsKey("SecretKey"))
            return null;

        return Convert.FromBase64String(response.Item["SecretKey"].S);
    }
}
