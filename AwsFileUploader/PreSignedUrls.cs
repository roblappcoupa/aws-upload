namespace AwsFileUploader;

public class PreSignedUrl
{
    public int Segment { get; set; }

    public string Url { get; set; }
}

public class PreSignedUrlRequest
{
    public int SegmentStart { get; set; }

    public int SegmentCount { get; set; }

    public Guid SessionId { get; set; }
}