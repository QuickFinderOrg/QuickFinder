using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class TestModel(ILogger<TestModel> logger, DiscordBotService discordBotService, IWebHostEnvironment environment) : PageModel
{
    private readonly ILogger<TestModel> _logger = logger;

    public string TestResult = "";

    public IActionResult OnGet()
    {
        if (environment.IsDevelopment() == false)
        {
            return Redirect("/");
        }

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
}

