using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace group_finder.Pages
{
    public class LoginModel() : PageModel
    {

        public IActionResult OnGet()
        {
            var user_session = HttpContext.GetUserId();
            if (user_session is null)
            {
                return Page();
            }
            return Redirect(StudentRoutes.Home());
            }

        public IActionResult OnPostDiscord()
        {
            var redirect_uri_raw = $"https://{HttpContext.Request.Host}/discord";
            var redirect_uri = System.Net.WebUtility.UrlEncode(redirect_uri_raw);
            var discord_url = $"https://discord.com/oauth2/authorize?client_id=1313080613980606514&response_type=code&redirect_uri={redirect_uri}&scope=identify+email";
            return Redirect(discord_url);
        }
    }
}