using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Teacher;

public class CourseOverviewModel(
    ILogger<CourseOverviewModel> logger,
    DiscordService discordService,
    CourseRepository courseRepository,
    GroupRepository groupRepository
) : PageModel
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
        Course = await courseRepository.GetByIdAsync(Course.Id);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(Guid id)
    {
        await groupRepository.DeleteGroup(id);
        Course = await courseRepository.GetByIdAsync(Course.Id);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostChangeGroupSIzeAsync()
    {
        await groupRepository.ChangeGroupSize(Course.Id, Course.GroupSize);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostChangeCustomGroupSizeAsync()
    {
        await groupRepository.ChangeCustomGroupSize(Course.Id, AllowCustomSize);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task LoadAsync(Guid courseId)
    {
        Courses = await courseRepository.GetAllAsync();
        if (courseId == Guid.Empty)
        {
            Course = Courses[0];
        }
        else
        {
            Course = await courseRepository.GetByIdAsync(courseId);
        }
        var grouplist = await groupRepository.GetGroups(Course.Id);
        var CourseDiscordServers = await discordService.GetCourseServer(Course.Id);
        CourseDiscordServer = CourseDiscordServers.FirstOrDefault();

        Groups = grouplist.ToList();
        AllowCustomSize = Course.AllowCustomSize;
        _logger.LogInformation("LoadGroups");
    }
}
