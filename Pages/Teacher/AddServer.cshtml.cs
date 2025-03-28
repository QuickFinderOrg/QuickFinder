using group_finder.Domain.DiscordDomain;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Teacher;

public class AddServerModel(ILogger<CreateCourseModel> logger, DiscordService discordService) : PageModel
{
    private readonly ILogger<CreateCourseModel> _logger = logger;
    public DiscordServerItem[] Servers = [];

    public string InviteURL = discordService.InviteURL;

    [BindProperty(SupportsGet = true)]
    public string CourseId { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        var discordIdClaim = User.FindFirst("discordId");
        if (discordIdClaim == null)
        {
            return RedirectToPage("/Login");
        }

        Load(ulong.Parse(discordIdClaim.Value));

        return Page();
    }

    public void Load(ulong discordId)
    {

        Servers = discordService.GetServersOwnedByUser(discordId);

    }


    public async Task<IActionResult> OnPostAsync(ulong serverId, Guid courseId)
    {
        var discordIdClaim = User.FindFirst("discordId");
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

        _logger.LogInformation("Server with ID {ServerId} has been added to course {CourseId}.", serverId, CourseId);

        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = CourseId });
    }
}