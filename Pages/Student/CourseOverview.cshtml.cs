using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class CourseOverviewModel(
    ILogger<CourseOverviewModel> logger,
    UserManager<User> userManager,
    CourseRepository courseRepository,
    GroupRepository groupRepository
) : PageModel
{
    private readonly ILogger<CourseOverviewModel> _logger = logger;

    [BindProperty]
    public Course Course { get; set; } = default!;

    [BindProperty]
    public Group[] Groups { get; set; } = [];

    [BindProperty]
    public Group Group { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid courseId)
    {
        var userId = userManager.GetUserId(User) ?? throw new Exception("User not found");
        var course = await courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            return Page();
        }
        Course = course;

        var groups = await groupRepository.GetGroups(courseId);
        if (groups.Length != 0)
        {
            var group = groups.FirstOrDefault(g => g.Members.Any(m => m.Id == userId));
            if (group != null)
            {
                Group = group;
            }
            groups = [.. groups.Where(g => !g.Members.Any(m => m.Id == userId))];
            Groups = groups;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostJoinGroupAsync(Guid groupId)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return NotFound();
        }

        await groupRepository.AddGroupMembersAsync(groupId, [user.Id]);

        return RedirectToPage(StudentRoutes.CourseOverview(), new { courseId = Course.Id });
    }

    public async Task<IActionResult> OnPostLeaveGroupAsync(Guid groupId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var group = await groupRepository.GetGroup(groupId);
        if (group == null)
        {
            return NotFound();
        }

        await groupRepository.RemoveGroupMembersAsync(groupId, [user.Id]);

        return RedirectToPage(StudentRoutes.CourseOverview(), new { courseId = Course.Id });
    }
}
