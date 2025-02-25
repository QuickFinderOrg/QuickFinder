using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages
{
    public class DiscordLoginModel(IConfiguration configuration) : PageModel
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
            var url = HttpContext.Request.GetDisplayUrl();
            Console.WriteLine(url);
            return Redirect("/");
        }


    }
}