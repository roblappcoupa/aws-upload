namespace AwsFileUploader;

using Flurl.Http;
using Microsoft.Extensions.Options;

public interface IUrlClient
{
    Task<IList<PreSignedUrl>> GetUrls(int segmentStart, int segmentCount, Guid sessionId);
}

public class UrlClient
{
    private readonly IOptions<AppConfiguration> options;

    public UrlClient(IOptions<AppConfiguration> options)
    {
        this.options = options;
    }

    public async Task<IList<PreSignedUrl>> GetUrls(int segmentStart, int segmentCount, Guid sessionId)
    {


        var batchOfUrls = await this.options.Value.AssetsHost
            .WithOAuthBearerToken(this.options.Value.AccessToken)
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