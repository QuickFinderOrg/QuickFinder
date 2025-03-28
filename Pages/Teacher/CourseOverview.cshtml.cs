using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages.Teacher;

public class CourseOverviewModel(ILogger<CourseOverviewModel> logger, MatchmakingService matchmaking, DiscordService discordService) : PageModel
{
    private readonly ILogger<CourseOverviewModel> _logger = logger;
    public List<Group> Groups = [];
    public Course[] Courses = [];
    [BindProperty]
    public Course Course { get; set; } = default!;

    public DiscordServerItem? CourseDiscordServer { get; set; }

    [BindProperty]
    public bool AllowCustomSize { get; set; }


    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostChangeCourseAsync()
    {
        Course = await matchmaking.GetCourse(Course.Id);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(Guid id)
    {
        await matchmaking.DeleteGroup(id);
        Course = await matchmaking.GetCourse(Course.Id);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostChangeGroupSIzeAsync()
    {
        await matchmaking.ChangeGroupSize(Course.Id, Course.GroupSize);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostChangeCustomGroupSizeAsync()
    {
        await matchmaking.ChangeCustomGroupSize(Course.Id, AllowCustomSize);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task LoadAsync(Guid courseId)
    {
        Courses = await matchmaking.GetCourses();
        if (courseId == Guid.Empty)
        {
            Course = Courses[0];
        }
        else
        {
            Course = await matchmaking.GetCourse(courseId);
        }
        var grouplist = await matchmaking.GetGroups(Course.Id);
        var CourseDiscordServers = await discordService.GetCourseServer(Course.Id);
        CourseDiscordServer = CourseDiscordServers.FirstOrDefault();

        Groups = grouplist.ToList();
        AllowCustomSize = Course.AllowCustomSize;
        _logger.LogInformation("LoadGroups");
    }
}


