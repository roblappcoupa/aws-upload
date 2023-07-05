namespace AwsFileUploader;

using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;

public interface ISessionClient
{
    Task<Guid> StartSession();

    Task CompleteSession(Guid sessionId);

    Task<IList<PreSignedUrl>> GetUrls(int segmentStart, int segmentCount, Guid sessionId);
}

internal sealed class SessionClient : ISessionClient
{
    private readonly ITokenClient tokenClient;
    private readonly IOptions<AppConfiguration> options;

    public SessionClient(
        ITokenClient tokenClient,
        IOptions<AppConfiguration> options)
    {
        this.tokenClient = tokenClient;
        this.options = options;
    }

    public async Task<Guid> StartSession()
    {
        var token = await this.tokenClient.GetAccessToken();

        var session = await this.options.Value.AssetsHost
            .AppendPathSegment("api/v5/upload/sessions/start")
            .WithOAuthBearerToken(token)
            .PostJsonAsync(
                new
                {
                    fileName = "SomeDummyFileNameForNow.mdf",
                    intent = "CsvImport",
                    sendNotification = false
                })
            .ReceiveJson<StartSessionResponse>();

        return session.SessionId;
    }

    public async Task CompleteSession(Guid sessionId)
    {
        var token = await this.tokenClient.GetAccessToken();

        var response = await this.options.Value.AssetsHost
            .AppendPathSegment("api/v5/upload/sessions")
            .AppendPathSegment(sessionId)
            .AppendPathSegment("complete")
            .WithOAuthBearerToken(token)
            .PutAsync();

        response.ResponseMessage.EnsureSuccessStatusCode();
    }

    public async Task<IList<PreSignedUrl>> GetUrls(int segmentStart, int segmentCount, Guid sessionId)
    {
        var token = await this.tokenClient.GetAccessToken();

        var batchOfUrls = await this.options.Value.AssetsHost
            .AppendPathSegment("api/v5/upload/sessions")
            .AppendPathSegment(sessionId)
            .AppendPathSegment("urls")
            .WithOAuthBearerToken(token)
            .PostJsonAsync(new PreSignedUrlRequest
            {
                SegmentStart = segmentStart,
                SegmentCount = segmentCount,
                SessionId = sessionId
            })
            .ReceiveJson<IList<PreSignedUrl>>();

        return batchOfUrls;
    }
}

public class StartSessionRequest
{
    public string FileName { get; set; }

    public string Intent { get; set; }

    public bool SendNotification { get; set; }
}

public class StartSessionResponse
{
    public Guid SessionId { get; set; }
}