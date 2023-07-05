namespace AwsFileUploader;

using System.Collections.Concurrent;
using Flurl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public interface IUrlProvider
{
    Task<string> GetUrl(Guid sessionId);
}

internal class UrlProvider : IUrlProvider
{
    private readonly ISessionClient sessionClient;
    private readonly IOptions<AppConfiguration> options;
    private readonly ILogger<UrlProvider> logger;

    private static readonly SemaphoreSlim semaphoreSlim = new(initialCount: 1);

    public UrlProvider(
        ISessionClient sessionClient,
        IOptions<AppConfiguration> options,
        ILogger<UrlProvider> logger)
    {
        this.sessionClient = sessionClient;
        this.options = options;
        this.logger = logger;

        this.CurrentSegmentStart = 0;
    }

    protected ConcurrentQueue<string> PreSignedUrls = new();

    protected int CurrentSegmentStart { get; private set; }

    protected int SegmentCount => this.options.Value.SegmentBatchSize;

    public async Task<string> GetUrl(Guid sessionId)
    {
        await semaphoreSlim.WaitAsync();

        try
        {
            if (this.PreSignedUrls.IsEmpty || !this.PreSignedUrls.TryPeek(out _))
            {
                var urls = await this.sessionClient.GetUrls(this.CurrentSegmentStart, this.SegmentCount, sessionId);

                foreach (var url in urls)
                {
                    this.logger.LogInformation("adding url segment {Url}", url.Segment);
                    this.PreSignedUrls.Enqueue(url.Url);
                }

                this.CurrentSegmentStart += urls.Count;
            }

            if (!this.PreSignedUrls.TryDequeue(out var result))
            {
                throw new Exception("Fatal error trying to get URLs");
            }

            this.logger.LogInformation("Returning {Url}", result);

            return result;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "An error occurred trying to get pre-signed URLs");

            throw;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
}