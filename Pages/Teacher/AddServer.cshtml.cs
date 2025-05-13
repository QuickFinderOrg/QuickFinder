using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;
using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Teacher;

public class AddServerModel(ILogger<AddServerModel> logger, DiscordService discordService)
    : PageModel
{
    public DiscordServerItem[] Servers = [];

    public string InviteURL = "";

    [BindProperty(SupportsGet = true)]
    public string CourseId { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        var discordIdClaim = User.FindFirst(ApplicationClaimTypes.DiscordId);
        if (discordIdClaim == null)
        {
            return RedirectToPage("/Login");
        }

        Load(ulong.Parse(discordIdClaim.Value));

        return Page();
    }

    public void Load(ulong discordId)
    {
        try
        {
            Servers = discordService.GetServersOwnedByUser(discordId);
            InviteURL = discordService.InviteURL;
        }
        catch (System.Exception)
        {
            PageContext.ViewData["ErrorMessage"] = "Could not load discord servers. ";
            logger.LogError("Could not load discord servers for user. Is Discord configured?");
        }
    }

    public async Task<IActionResult> OnPostAsync(ulong serverId, Guid courseId)
    {
        var discordIdClaim = User.FindFirst(ApplicationClaimTypes.DiscordId);
        if (discordIdClaim == null)
        {
            return RedirectToPage("/Login");
        }
        Load(ulong.Parse(discordIdClaim.Value));

        if (serverId == 0)
        {
            return Page();
        }

        await discordService.EnsureCourseServer(courseId, serverId);

        logger.LogInformation(
            "Server with ID {ServerId} has been added to course {CourseId}.",
            serverId,
            CourseId
        );

        return RedirectToPage(TeacherRoutes.CourseOverview(), new { courseId = CourseId });
    }
}
