namespace AwsFileUploader;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public interface IUrlProvider
{
    Task<string> GetUrl();
}

internal class UrlProvider : IUrlProvider
{
    private readonly IUrlClient urlClient;
    private readonly IOptions<AppConfiguration> options;
    private readonly ILogger<UrlProvider> logger;

    private static readonly SemaphoreSlim semaphoreSlim = new(initialCount: 1);

    public UrlProvider(
        IUrlClient urlClient,
        IOptions<AppConfiguration> options,
        ILogger<UrlProvider> logger)
    {
        this.urlClient = urlClient;
        this.options = options;
        this.logger = logger;

        this.CurrentSegmentStart = 0;
    }

    protected ConcurrentQueue<string> PreSignedUrls = new();

    protected int CurrentSegmentStart { get; private set; }

    protected int SegmentCount => this.options.Value.SegmentBatchSize;

    protected Guid SessionId => this.options.Value.SessionId;

    public async Task<string> GetUrl()
    {
        await semaphoreSlim.WaitAsync();

        try
        {
            if (this.PreSignedUrls.IsEmpty || !this.PreSignedUrls.TryDequeue(out _))
            {
                var urls = await this.urlClient.GetUrls(this.CurrentSegmentStart, this.SegmentCount, this.SessionId);

                foreach (var url in urls)
                {
                    this.PreSignedUrls.Enqueue(url.Url);
                }

                this.CurrentSegmentStart += urls.Count;
            }

            if (!this.PreSignedUrls.TryDequeue(out var result))
            {
                throw new Exception("Fatal error trying to get URLs");
            }

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