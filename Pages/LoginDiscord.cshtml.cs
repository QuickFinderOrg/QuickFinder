using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QuickFinder.Domain.DiscordDomain;

namespace QuickFinder.Pages;

public class DiscordModel(
    UserService userService,
    SignInManager<User> signInManager,
    IOptions<DiscordServiceOptions> options
) : PageModel
{
    private const string API_ENDPOINT = "https://discord.com/api/v10";
    private readonly string CLIENT_ID = options.Value.ClientId;
    private readonly string CLIENT_SECRET = options.Value.ClientSecret;

    public async Task<IActionResult> OnGetAsync(string code)
    {
        Console.WriteLine("Code: " + code);
        {
            var responseJSON = await ExchangeCodeAsync(code);
            var tokenResponse =
                JsonSerializer.Deserialize<Dictionary<string, object>>(responseJSON)
                ?? throw new Exception("responseJSON");
            // within this token lies the power to surpass metal gear
            var token = tokenResponse["access_token"].ToString();
            if (token is null)
            {
                TempData["AlertDanger"] = "Something failed. You are not logged in.";
                return Redirect(StudentRoutes.Login());
            }

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token
            );
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

            if (user is not null)
            {
                await signInManager.SignInAsync(user, true);
                await userService.SetDiscordToken(user, token);
                TempData["AlertSuccess"] = "You have been logged in.";
                // Redirect to the home page or another page
                return Redirect(StudentRoutes.Home());
            }
            else
            {
                TempData["AlertDanger"] = "Something failed. You are not logged in.";
                return Redirect(StudentRoutes.Login());
            }

            // user.register(email, name, etc.)
            // don't create passwords for discord users
            // store discord id and access token
            // user.set profile
        }
    }

    public async Task<string> ExchangeCodeAsync(string code)
    {
        using HttpClient client = new HttpClient();
        var data = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>(
                    "redirect_uri",
                    $"https://{HttpContext.Request.Host}/LoginDiscord"
                ),
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
        Console.WriteLine(await response.Content.ReadAsStringAsync());

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
