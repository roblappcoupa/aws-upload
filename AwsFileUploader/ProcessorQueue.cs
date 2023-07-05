namespace AwsFileUploader;

using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public interface IProcessorQueue
{
    Task<Guid> Start();

    Task Enqueue(ProcessChunkRequest input);

    Task Stop(Guid sessionId);
}

public class ProcessorQueue : IProcessorQueue
{
    public ProcessorQueue(
        ISessionClient sessionClient,
        IUrlProvider urlProvider,
        ILogger<ProcessorQueue> logger,
        IOptions<AppConfiguration> options)
    {
        this.IsRunning = false;

        this.SessionClient = sessionClient;
        this.UrlProvider = urlProvider;
        this.Logger = logger;
        this.Options = options;

        this.ProcessingBlock = new ActionBlock<ProcessChunkRequest>(
            async input =>
            {
                try
                {
                    await this.Process(input);
                }
                catch (Exception exception)
                {
                    this.Logger.LogError(exception, "An Exception was thrown during the processing pipeline");

                    throw;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                //EnsureOrdered = true,
                SingleProducerConstrained = true // TODO: Experiment with this
            });
    }

    public bool IsRunning { get; private set; }

    protected ActionBlock<ProcessChunkRequest> ProcessingBlock { get; }

    protected ISessionClient SessionClient { get; }

    protected IUrlProvider UrlProvider { get; }

    protected IOptions<AppConfiguration> Options { get; }

    protected ILogger<ProcessorQueue> Logger { get;  }

    public async Task<Guid> Start()
    {
        if (this.IsRunning)
        {
            throw new Exception("You cannot start the pipeline because it was already running");
        }

        this.IsRunning = true;

        var sessionId = await this.SessionClient.StartSession();

        return sessionId;
    }

    public async Task Enqueue(ProcessChunkRequest input)
    {
        if (!this.IsRunning)
        {
            throw new Exception("You cannot use the pipeline because it was not started");
        }

        if (!await this.ProcessingBlock.SendAsync(input))
        {
            throw new Exception("Failed to queue batch");
        }
    }

    public async Task Stop(Guid sessionId)
    {
        if (!this.IsRunning)
        {
            throw new Exception("You cannot stop the pipeline because it was not started");
        }

        this.IsRunning = false;

        this.Logger.LogDebug("Calling Complete on the buffer block");
        this.ProcessingBlock.Complete();
        this.Logger.LogDebug("Done calling Complete on the buffer block");

        this.Logger.LogDebug("Awaiting Completion task on processing block");
        await this.ProcessingBlock.Completion;
        this.Logger.LogDebug("Done awaiting Completion task on processing block");

        this.Logger.LogDebug("Completing upload session {SessionId}", sessionId);
        //await this.SessionClient.CompleteSession(sessionId); // Don't complete upload session because it will kick off CsvImporter Joule app
        this.Logger.LogDebug("Completed upload session {SessionId}", sessionId);
    }

    private async Task Process(ProcessChunkRequest chunk)
    {
        var url = await this.UrlProvider.GetUrl(chunk.SessionId);

        using var stream = new MemoryStream();

        await stream.WriteAsync(chunk.Buffer, 0, chunk.Count);

        stream.Seek(0, SeekOrigin.Begin);

        using var streamContent = new DoNotDisposeStreamContent(stream);

        this.Logger.LogInformation("Uploading chunk {0}", chunk.ChunkId);

        var flurlRequest = new FlurlRequest(url);

        var response = await flurlRequest.PutAsync(streamContent);

        response.ResponseMessage.EnsureSuccessStatusCode();

        this.Logger.LogInformation(
            "Uploaded chunk {0}. Response status code: {1}",
            chunk.ChunkId,
            response.ResponseMessage.StatusCode);
    }

    internal sealed class DoNotDisposeStreamContent : StreamContent
    {
        public DoNotDisposeStreamContent(Stream stream)
            : base(stream)
        {
        }

        public DoNotDisposeStreamContent(Stream stream, int bufferSize)
            : base(stream, bufferSize)
        {
        }

        public bool DisposeStream { get; set; }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (this.DisposeStream)
            {
                base.Dispose(disposing);
            }
        }
    }
}