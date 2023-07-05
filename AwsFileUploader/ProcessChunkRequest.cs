namespace AwsFileUploader;

public class ProcessChunkRequest
{
    public Guid SessionId { get; set; }

    public string ChunkId { get; set; }

    public int Count { get; set; }

    public byte[] Buffer { get; set; }
}