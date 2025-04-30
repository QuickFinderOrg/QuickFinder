using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QuickFinder.Options;

namespace QuickFinder;

public class DiscordAuthHandler(
    UserService userService,
    SignInManager<User> signInManager,
    IOptions<DiscordAuthOptions> options,
    ILogger<DiscordAuthHandler> logger,
    IHttpClientFactory httpClientFactory
)
{
    private readonly DiscordAuthOptions options = options.Value;
    private readonly string[] ScopeList = ["identify", "email", "guilds.join"];
    private const string API_ENDPOINT = "https://discord.com/api/v10";
    private readonly string CLIENT_ID = options.Value.ClientId;
    private readonly string CLIENT_SECRET = options.Value.ClientSecret;

    public bool IsEnabled => options.IsValid;

    public string CreateAuthUrl(string host)
    {
        var clientId = options.ClientId;
        var scope = string.Join("+", ScopeList);
        var redirect_uri = System.Net.WebUtility.UrlEncode(CreateRedirectUri(host));
        var discord_url =
            $"https://discord.com/oauth2/authorize?client_id={clientId}&response_type=code&redirect_uri={redirect_uri}&scope={scope}";
        return discord_url;
    }

    private string CreateRedirectUri(string host)
    {
        return $"{options.RedirectScheme}://{host}/LoginDiscord";
    }

    public async Task Authenticate(string code, string host)
    {
        using HttpClient client = httpClientFactory.CreateClient();

        var tokenResponse = await ExchangeCodeAsync(code, host);
        var token = tokenResponse.AccessToken;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var identity_response = await client.GetAsync($"{API_ENDPOINT}/users/@me");
        identity_response.EnsureSuccessStatusCode();

        var identityJSON = await identity_response.Content.ReadAsStringAsync();
        var identity = JsonSerializer.Deserialize<DiscordUserIdentity>(identityJSON);

        if (identity?.Id == null)
        {
            logger.LogError("Discord identity response missing id.");
            throw new Exception("Invalid Discord identity response.");
        }

        if (identity?.Email == null)
        {
            logger.LogError("Discord identity response missing email (is email scope missing?).");
            throw new Exception("Invalid Discord identity response.");
        }

        var user = await userService.GetUserByDiscordId(identity.Email, identity.Id);

        user ??= await userService.CreateDiscordUser(
            identity.Email,
            identity.Username,
            identity.Id,
            identity.GlobalName
        );

        await signInManager.SignInAsync(user, true);
        await userService.SetDiscordToken(user, token);
    }

    public async Task<DiscordTokenResponse> ExchangeCodeAsync(string code, string host)
    {
        using HttpClient client = httpClientFactory.CreateClient();
        var data = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", CreateRedirectUri(host)),
            }
        );

        var authValue = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}")
        );
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            authValue
        );
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded")
        );

        var response = await client.PostAsync($"{API_ENDPOINT}/oauth2/token", data);

        response.EnsureSuccessStatusCode();

        var responseJSON = await response.Content.ReadAsStringAsync();

        var tokenResponse = JsonSerializer.Deserialize<DiscordTokenResponse>(responseJSON);

        if (tokenResponse == null)
        {
            throw new Exception("TokenResponse could not be deserialized");
        }

        return tokenResponse;
    }
}

// Models for Discord API responses
public class DiscordTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; set; }

    [JsonPropertyName("scope")]
    public required string Scope { get; set; }
}

public class DiscordUserIdentity
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("username")]
    public required string Username { get; set; }

    [JsonPropertyName("global_name")]
    public required string GlobalName { get; set; }

    [JsonPropertyName("avatar")]
    public required string Avatar { get; set; }

    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }
}
