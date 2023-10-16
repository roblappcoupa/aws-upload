namespace AwsFileUploader;

public class ProcessChunkRequest
{
    public Guid SessionId { get; init; }

    public string ChunkId { get; init; }

    public int Count { get; init; }

    public byte[] Buffer { get; init; }

    public string Url { get; init; }
}