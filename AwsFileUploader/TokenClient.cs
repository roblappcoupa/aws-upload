namespace AwsFileUploader;

using System.Collections.Concurrent;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

public interface ITokenClient
{
    Task<string> GetAccessToken();
}

internal sealed class CachingTokenClient : ITokenClient
{
    private readonly IOptions<AppConfiguration> options;

    private static readonly IDictionary<string, TokenCacheItem> Cache = new ConcurrentDictionary<string, TokenCacheItem>();

    public CachingTokenClient(IOptions<AppConfiguration> options)
    {
        this.options = options;
    }

    public async Task<string> GetAccessToken()
    {
        if (Cache.TryGetValue(this.options.Value.UserName, out var item) && item.Exp > DateTime.UtcNow)
        {
            return item.Token;
        }

        var rawResponse = await this.options.Value.AuthenticationHost
            .AppendPathSegment("connect/token")
            .PostUrlEncodedAsync(
                new
                {
                    client_id = this.options.Value.ClientId,
                    client_secret = this.options.Value.ClientSecret,
                    grant_type = "impersonation",
                    scope = "openid profile llamasoft_platform",
                    impersonate_user = this.options.Value.UserName
                });

        var rawString = await rawResponse.ResponseMessage.Content.ReadAsStringAsync();

        if (rawString == null)
        {
            throw new Exception("Authentication response was null");
        }

        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(rawString);

        var cachedItem = new TokenCacheItem
        {
            Exp = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30),
            Token = tokenResponse.AccessToken
        };

        Cache.Add(this.options.Value.UserName, cachedItem);

        return cachedItem.Token;
    }
}

public class TokenCacheItem
{
    public DateTime Exp { get; set; }

    public string Token { get; set; }
}

public class TokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("expires_in")]
    public long ExpiresIn { get; set; }
}