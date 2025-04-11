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

    public async Task<IActionResult> OnGetAsync(Guid courseId)
    {
        var userId = userManager.GetUserId(User) ?? throw new Exception("User not found");
        var course = await courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }
        Course = course;

        var groups = await groupRepository.GetGroups(courseId);
        groups = [.. groups.Where(g => g.Members.Any(m => m.Id == userId))];
        if (groups.Length != 0)
        {
            Groups = groups;
        }

        return Page();
    }
}
