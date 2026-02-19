using System.Collections.Generic;

public class CloudFrontEvent
{
    public List<CloudFrontRecord> Records { get; set; }
}

public class CloudFrontRecord
{
    public CloudFrontData Cf { get; set; }
}

public class CloudFrontData
{
    public CloudFrontRequest Request { get; set; }
}

public class CloudFrontRequest
{
    public Dictionary<string, List<CloudFrontHeader>> Headers { get; set; }
}

public class CloudFrontResponse
{
    public string Status { get; set; }
    public string StatusDescription { get; set; }
    public Dictionary<string, IList<CloudFrontHeader>> Headers { get; set; }
}

public class CloudFrontHeader
{
    public string Key { get; set; }
    public string Value { get; set; }
}
