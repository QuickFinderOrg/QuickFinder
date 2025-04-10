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
        await LoadAsync(courseId);
        return Page();
    }

    public async Task LoadAsync(Guid courseId)
    {
        Course = await courseRepository.GetCourse(courseId);
    }

}