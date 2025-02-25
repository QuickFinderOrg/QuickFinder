using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages
{
    public class LoginModel(IConfiguration configuration) : PageModel
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

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPost()
        {
            var query = new QueryBuilder();
            var url = new UriBuilder(AuthorizationEndpoint);
            query.Add("client_id", configuration[Constants.DiscordClientIdKey] ?? throw new Exception(Constants.DiscordClientIdKey));
            query.Add("response_type", "code");
            query.Add("scope", "identify email");
            query.Add("redirect_uri", $"{Request.Scheme}://{Request.Host}{CallbackPath}");
            query.Add("state", "solid");

            url.Query = query.ToString();

            if (!ModelState.IsValid)
            {
                return Page();

            }
            Console.WriteLine(url.ToString());
            return Redirect(url.ToString());
        }
    }
}