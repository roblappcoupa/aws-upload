namespace AwsFileUploader;

public class ProcessChunkRequest
{
    public string ChunkId { get; set; }

    public int Count { get; set; }

    public byte[] Buffer { get; set; }
}