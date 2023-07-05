namespace AwsFileUploader;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AppRunner
{
    private readonly IProcessorQueue processorQueue;
    private readonly IOptions<AppConfiguration> options;
    private readonly ILogger<AppRunner> logger;

    public AppRunner(
        IProcessorQueue processorQueue,
        IOptions<AppConfiguration> options,
        ILogger<AppRunner> logger)
    {
        this.processorQueue = processorQueue;
        this.options = options;
        this.logger = logger;
    }

    public async Task Run()
    {
        var timer = new Stopwatch();

        timer.Start();

        this.logger.LogInformation("Starting upload process");
        this.logger.LogInformation("Processing file {0} for upload", this.options.Value.FilePath);

        var fileInfo = new FileInfo(this.options.Value.FilePath);

        await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);

        var fileSize = fileStream.Length;

        var totalChunks = CalculateTotalNumberOfChunks(
            fileSize,
            this.options.Value.ChunkSize);

        var remainingChunks = totalChunks;

        this.logger.LogInformation(
            "Calculated chunks. File length: {0}, Number of chunks: {1}, Chunk size: {2}\n\n",
            fileSize,
            totalChunks,
            this.options.Value.ChunkSize);

        var sessionId = await this.processorQueue.Start();
        this.logger.LogInformation("Started session {SessionId}", sessionId);

        for (var i = 0; i < totalChunks; i++)
        {
            var chunkNumber = i + 1;

            this.logger.LogInformation(
                "Processing chunk {0}/{1}",
                chunkNumber,
                totalChunks);

            var buffer = new byte[this.options.Value.ChunkSize];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, this.options.Value.ChunkSize);

            await this.processorQueue.Enqueue(
                new ProcessChunkRequest
                {
                    SessionId = sessionId,
                    ChunkId = chunkNumber.ToString(),
                    Count = bytesRead,
                    Buffer = buffer
                });

            remainingChunks--;

            this.logger.LogInformation(
                "Completed processing chunk {0}/{1}. Remaining chunks: {2}",
                chunkNumber,
                totalChunks,
                remainingChunks);
        }

        this.logger.LogInformation("Processed all {0} chunks", totalChunks);

        await this.processorQueue.Stop(sessionId);

        timer.Stop();

        this.logger.LogInformation("Completed upload process. Took: {0:g}", timer.Elapsed);
    }

    private static int CalculateTotalNumberOfChunks(long streamLength, int chunkSize)
    {
        var numberOfWholeChunks = (int)(streamLength / chunkSize);
        var numberOfPartialChunks = streamLength % chunkSize > 0 ? 1 : 0;
        return numberOfWholeChunks + numberOfPartialChunks;
    }
}