using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class CourseOverviewModel(ILogger<CourseOverviewModel> logger, MatchmakingService matchmaking) : PageModel
{
    private readonly ILogger<CourseOverviewModel> _logger = logger;

    [BindProperty]
    public Course Course { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadAsync(id);
        return Page();
    }

    public async Task LoadAsync(Guid courseId)
    {
        Course = await matchmaking.GetCourse(courseId);
    }

}