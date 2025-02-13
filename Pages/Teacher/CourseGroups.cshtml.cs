using System.ComponentModel.DataAnnotations;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class CourseGroupsModel(ILogger<CourseGroupsModel> logger, MatchmakingService matchmaking) : PageModel
{
    private readonly ILogger<CourseGroupsModel> _logger = logger;

    public List<Group> Groups = [];
    
    [BindProperty]
    public string CourseName { get; set; } = "DAT120";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync(CourseName);
        return Page();
    }

    public async Task<IActionResult> OnPostChangeCourseAsync()
    {
        await LoadAsync(CourseName);
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
        await LoadAsync(CourseName);
        return Page();
    }

    public async Task LoadAsync(string courseName)
    {
        var grouplist = await matchmaking.GetGroups(courseName);
        Groups = grouplist.ToList();
        _logger.LogInformation("LoadAsync");
    }
}


