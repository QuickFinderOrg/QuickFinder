using QuickFinder.Domain.DiscordDomain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages.Admin;

public class ServersModel(ILogger<StudentsModel> logger, DiscordService discordService) : PageModel
{
    private readonly ILogger<StudentsModel> _logger = logger;

    public DiscordServerItem[] Servers = [];
    public string InviteURL = "";

    [BindProperty]
    public string SearchQuery { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        Servers = await discordService.GetServerList();
        InviteURL = discordService.InviteURL;
        return Page();
    }


}


