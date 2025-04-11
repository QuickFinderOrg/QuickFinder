using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.DiscordDomain;

namespace QuickFinder.Pages;

[Authorize(Policy = "Admin")]
public class TestModel(
    ILogger<TestModel> logger,
    DiscordService discordBotService,
    IWebHostEnvironment environment,
    UserService userService,
    UserManager<User> userManager
) : PageModel
{
    private readonly ILogger<TestModel> _logger = logger;

    public string TestResult = "";

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteChannelAsync(string id)
    {
        if (environment.IsDevelopment() == false)
        {
            return Redirect("/");
        }

        if (string.IsNullOrWhiteSpace("id"))
        {
            return RedirectToPage();
        }

        _logger.LogInformation("POST: test");
        var channelId = await discordBotService.DeleteChannel(ulong.Parse(id));
        TestResult = $"channel {channelId}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetChannelPermissionsAsync(string id)
    {
        if (environment.IsDevelopment() == false)
        {
            return Redirect("/");
        }

        if (string.IsNullOrWhiteSpace("id"))
        {
            return RedirectToPage();
        }

        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            _logger.LogError("User not logged in");
            return RedirectToPage();
        }

        var discord_user_id = await userService.GetDiscordId(user.Id);
        if (discord_user_id == null)
        {
            _logger.LogError("Discord id not found");
            return RedirectToPage();
        }

        _logger.LogInformation("POST: update permissions for {channelId}", id);
        var channelId = await discordBotService.SetUserPermissionsOnChannel(
            ulong.Parse(id),
            (ulong)discord_user_id
        );
        if (channelId == null)
        {
            _logger.LogError("Failed to set permsissions");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddServerAsync(string serverId)
    {
        if (string.IsNullOrWhiteSpace("id"))
        {
            return RedirectToPage();
        }

        _logger.LogInformation("POST: test");
        await discordBotService.AddServer(ulong.Parse(serverId));
        return RedirectToPage();
    }
}
