using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class CourseOverviewModel(
    ILogger<CourseOverviewModel> logger,
    CourseRepository courseRepository
    ) : PageModel
{
    private readonly ILogger<CourseOverviewModel> _logger = logger;

    [BindProperty]
    public Course Course { get; set; } = default!;


    public async Task<IActionResult> OnGetAsync(Guid courseId)
    {
        var course = await courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }
        return Page();
    }
}