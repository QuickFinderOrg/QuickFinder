using System.Net.Http.Headers;
using System.Text;
using group_finder.Domain.DiscordDomain;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace group_finder.Pages
{
    public class DiscordLoginModel(IOptions<DiscordServiceOptions> options) : PageModel
    {
        /// <summary>
        /// Default value for <see cref="AuthenticationScheme.DisplayName"/>.
        /// </summary>
        public static readonly string DisplayName = "Discord";

        /// <summary>
        /// Default value for <see cref="AuthenticationSchemeOptions.ClaimsIssuer"/>.
        /// </summary>
        public static readonly string Issuer = "Discord";

        /// <summary>
        /// Default value for <see cref="RemoteAuthenticationOptions.CallbackPath"/>.
        /// </summary>
        public static readonly string CallbackPath = "/discord-login";

        /// <summary>
        /// Default value for <see cref="OAuthOptions.AuthorizationEndpoint"/>.
        /// </summary>
        public static readonly string AuthorizationEndpoint = "https://discord.com/api/oauth2/authorize";

        /// <summary>
        /// Default value for <see cref="OAuthOptions.TokenEndpoint"/>.
        /// </summary>
        public static readonly string TokenEndpoint = "https://discord.com/api/oauth2/token";

        /// <summary>
        /// Default value for <see cref="OAuthOptions.UserInformationEndpoint"/>.
        /// </summary>
        public static readonly string UserInformationEndpoint = "https://discord.com/api/users/@me";

        public async Task<IActionResult> OnGetAsync()
        {
            var url = HttpContext.Request.GetDisplayUrl();
            var code = HttpContext.Request.Query["code"];
            var state = HttpContext.Request.Query["state"]; //TODO: use state for redirecting
            Console.WriteLine(url);

            if (Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(code))
            {
                throw new Exception("Code is null");
            }

            var token = await ExchangeCodeAsync(code!);
            Console.WriteLine($"Discord Token Recieved");

            var UserInfo = await GetUserInformationAsync(token);
            Console.WriteLine($"User info for {UserInfo.DisplayName} ({UserInfo.Username}) got");
            return Redirect("/");
        }

        public async Task<string> ExchangeCodeAsync(string code)
        {
            using var client = new HttpClient();
            var data = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", $"{Request.Scheme}://{Request.Host}{CallbackPath}")
            ]);

            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Value.ClientId}:{options.Value.ClientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            HttpResponseMessage response = await client.PostAsync(TokenEndpoint, data);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            response.EnsureSuccessStatusCode();

            var token_json_string = await response.Content.ReadAsStringAsync();

            var tokenData = System.Text.Json.JsonDocument.Parse(token_json_string);
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("access_token is null");
            }

            // TODO: store refresh token

            return accessToken;
        }

        public async Task<DiscordUserVM> GetUserInformationAsync(string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.GetAsync(UserInformationEndpoint);
            response.EnsureSuccessStatusCode();

            var response_json_string = await response.Content.ReadAsStringAsync();

            var user = System.Text.Json.JsonDocument.Parse(response_json_string);

            return new DiscordUserVM()
            {
                Id = user.RootElement.GetProperty("id").GetString()!,
                Username = user.RootElement.GetProperty("username").GetString()!,
                DisplayName = user.RootElement.GetProperty("global_name").GetString(),
                Email = user.RootElement.GetProperty("email").GetString(),
                IsEmailVerified = user.RootElement.GetProperty("verified").GetBoolean()
            };
        }

        public record class DiscordUserVM
        {
            public required string Id;
            public required string Username;
            public required string? DisplayName;
            public required string? Email;
            public required bool? IsEmailVerified;
        }
    }

}