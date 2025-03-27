using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages
{
    public class LoginModel() : PageModel
    {

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPostDiscord()
        {
            var redirect_uri_raw = $"https://{HttpContext.Request.Host}/LoginDiscord";
            var redirect_uri = System.Net.WebUtility.UrlEncode(redirect_uri_raw);
            var discord_url = $"https://discord.com/oauth2/authorize?client_id=1328341129078505499&response_type=code&redirect_uri={redirect_uri}&scope=identify+email+guilds";
            return Redirect(discord_url);
        }
    }
}