using Amazon.CloudFront;
using Amazon.CloudFront.Model;

namespace CF_Cache_API.Services;

public class CloudFrontService
{
    private readonly string _distributionId = "E1X7R3DVK2IL9";

    public async Task InvalidateCacheAsync(int employeeId, string tenantId)
    {
        var client = new AmazonCloudFrontClient(Amazon.RegionEndpoint.USEast1);
        
        var paths = new List<string>
        {
            $"/api/employee/{tenantId}/{employeeId}",
            $"/api/employee/{tenantId}*"
        };

        var request = new CreateInvalidationRequest
        {
            DistributionId = _distributionId,
            InvalidationBatch = new InvalidationBatch
            {
                CallerReference = $"update-{employeeId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Paths = new Paths
                {
                    Quantity = paths.Count,
                    Items = paths
                }
            }
        };

        await client.CreateInvalidationAsync(request);
    }
}
