using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages;

public class DiscordModel(DiscordAuthHandler discordAuthHandler, ILogger<DiscordModel> logger)
    : PageModel
{
    public async Task<IActionResult> OnGetAsync(string code)
    {
        logger.LogInformation("Discord auth redirect");
        try
        {
            // Needed for redirecting to the correct url after auth
            var host = HttpContext.Request.Host.ToString();
            await discordAuthHandler.Authenticate(code, host);

            TempData["AlertSuccess"] = "You have been logged in.";
            return Redirect(StudentRoutes.Home());
        }
        catch (Exception e)
        {
            TempData["AlertDanger"] = "Something failed. You are not logged in.";
            logger.LogError(e, "Error while authenticating with Discord.");
            return Redirect(StudentRoutes.Login());
        }
    }
}
