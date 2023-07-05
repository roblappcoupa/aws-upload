﻿namespace AwsFileUploader;

public class AppConfiguration
{
    public string FilePath { get; set; }

    public int ChunkSize { get; set; } = 1024 * 1024 * 5; // 5242880 bytes

    public string AssetsHost { get; set; }

    public string AuthenticationHost { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string UserName { get; set; }

    public int SegmentBatchSize { get; set; } = 1000;

    public Guid SessionId { get; set; }
}