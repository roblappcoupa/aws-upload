using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AwsFileUploader;

public interface ITokenClient
{
    Task<TokenResponse> GetAccessToken();
}

public class TokenClient : ITokenClient
{
    private readonly IOptionsMonitor<AppConfiguration> options;

    public TokenClient(IOptionsMonitor<AppConfiguration> options)
    {
        this.options = options;
    }

    public async Task<TokenResponse> GetAccessToken()
    {
        var rawResponse = await this.options.CurrentValue.AuthenticationHost
            .AppendPathSegment("connect/token")
            .PostUrlEncodedAsync(
                new
                {
                    client_id = this.options.CurrentValue.ClientId,
                    client_secret = this.options.CurrentValue.ClientSecret,
                    grant_type = "impersonation",
                    scope = "openid profile llamasoft_platform scg_ws_api",
                    impersonate_user = this.options.CurrentValue.UserName
                });

        var rawString = await rawResponse.ResponseMessage.Content.ReadAsStringAsync();

        if (rawString == null)
        {
            throw new Exception("Authentication response was null");
        }

        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(rawString);

        return tokenResponse;
    }
}

public class TokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("expires_in")]
    public long ExpiresIn { get; set; }
}