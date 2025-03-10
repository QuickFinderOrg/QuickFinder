using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class TestModel(ILogger<TestModel> logger, DiscordBotService discordBotService, IWebHostEnvironment environment) : PageModel
{
    private readonly ILogger<TestModel> _logger = logger;

    public string TestResult = "";

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (environment.IsDevelopment() == false)
        {
            return Redirect("/");
        }
        _logger.LogInformation("POST: test");
        var channelId = await discordBotService.CreateChannel("channel-5");
        TestResult = $"channel {channelId}";
        return RedirectToPage();
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
}

