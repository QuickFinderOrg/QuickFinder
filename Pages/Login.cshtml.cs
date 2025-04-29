using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages
{
    public class LoginModel(DiscordAuthHandler discordAuth) : PageModel
    {
        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPostDiscord()
        {
            return Redirect(discordAuth.CreateAuthUrl(HttpContext.Request.Host.ToString()));
        }
    }
}
