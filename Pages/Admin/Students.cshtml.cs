using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Admin;

public class StudentsModel(ILogger<StudentsModel> logger, MatchmakingService matchmaking, UserService userService) : PageModel
{
    private readonly ILogger<StudentsModel> _logger = logger;

    public List<Ticket> Students = [];
    public List<Group> Groups = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMatchAsync()
    {
        await matchmaking.DoMatching();
        await LoadAsync();
        return Page();

    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        await matchmaking.Reset();
        var courses = await matchmaking.GetCourses();
        var course = courses[0];
        var users = await userService.GetAllUsers();
        foreach (var user in users)
        {
            await matchmaking.AddToWaitlist(user, course);
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(Guid id)
    {
        var users = await matchmaking.GetGroupMembers(id);
        await matchmaking.DeleteGroup(id);
        var courses = await matchmaking.GetCourses();
        var course = courses[0];
        foreach (var user in users)
        {
            await matchmaking.AddToWaitlist(user, course);
        }
        await LoadAsync();
        return Page();
    }

    public async Task LoadAsync()
    {
        var waitlist = await matchmaking.GetWaitlist();
        foreach (Ticket ticket in waitlist)
        {
            Students.Add(ticket);
        }
        var grouplist = await matchmaking.GetGroups();
        Groups = grouplist.ToList();
        _logger.LogInformation("LoadAsync");
    }
}


