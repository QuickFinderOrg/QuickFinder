using System.ComponentModel.DataAnnotations;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Teacher;

public class CourseGroupsModel(ILogger<CourseGroupsModel> logger, MatchmakingService matchmaking) : PageModel
{
    private readonly ILogger<CourseGroupsModel> _logger = logger;
    public List<Group> Groups = [];
    public Course[] Courses = [];
    [BindProperty]
    public Course Course {get; set;} = default!;


    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostChangeCourseAsync()
    {
        Course = await matchmaking.GetCourse(Course.Id);
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(Guid id)
    {
        var users = await matchmaking.GetGroupMembers(id);
        await matchmaking.DeleteGroup(id);
        Courses = await matchmaking.GetCourses();
        var course = Courses[0];
        foreach (var user in users)
        {
            await matchmaking.AddToWaitlist(user, course);
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostChangeGroupSIzeAsync()
    {        
        await matchmaking.ChangeGroupSize(Course.Id, Course.GroupSize);
        await LoadAsync();
        return Page();
    }

    public async Task LoadAsync()
    {
        Courses = await matchmaking.GetCourses();
        Course ??= Courses[0];
        var grouplist = await matchmaking.GetGroups(Course.Id);
        Groups = grouplist.ToList();
        _logger.LogInformation("LoadGroups");
    }
}


