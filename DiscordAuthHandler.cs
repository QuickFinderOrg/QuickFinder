using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QuickFinder.Options;

namespace QuickFinder;

public class DiscordAuthHandler(
    UserService userService,
    SignInManager<User> signInManager,
    IOptions<DiscordAuthOptions> options,
    ILogger<DiscordAuthHandler> logger
)
{
    private readonly DiscordAuthOptions options = options.Value;
    private readonly string[] ScopeList = ["identify", "email", "guilds", "guilds.join"];
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
        var responseJSON = await ExchangeCodeAsync(code, host);
        var tokenResponse =
            JsonSerializer.Deserialize<Dictionary<string, object>>(responseJSON)
            ?? throw new Exception("responseJSON");
        // within this token lies the power to surpass metal gear
        var token = tokenResponse["access_token"].ToString();

        if (token is null)
        {
            throw new NullReferenceException(nameof(token));
        }

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var identity_response = await client.GetAsync("https://discord.com/api/v10/users/@me");
        var identityJSON = await identity_response.Content.ReadAsStringAsync();

        var identityDict =
            JsonSerializer.Deserialize<Dictionary<string, object>>(identityJSON)
            ?? throw new Exception("responseJSON");

        var user = await userService.GetUserByDiscordId(
            identityDict["email"].ToString() ?? throw new Exception("email"),
            identityDict["id"].ToString() ?? throw new Exception("id")
        );

        user ??= await userService.CreateDiscordUser(
            identityDict["email"].ToString() ?? throw new Exception("email"),
            identityDict["username"].ToString() ?? throw new Exception("username"),
            identityDict["id"].ToString() ?? throw new Exception("id"),
            identityDict["global_name"].ToString() ?? throw new Exception("display name")
        );

        if (user == null)
        {
            throw new Exception("User is null");
        }

        await signInManager.SignInAsync(user, true);
        await userService.SetDiscordToken(user, token);
    }

    public async Task<string> ExchangeCodeAsync(string code, string host)
    {
        using HttpClient client = new HttpClient();
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

        HttpResponseMessage response = await client.PostAsync($"{API_ENDPOINT}/oauth2/token", data);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
