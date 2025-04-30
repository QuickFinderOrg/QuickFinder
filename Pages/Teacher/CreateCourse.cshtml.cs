using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Teacher;

public class CreateCourseModel(ILogger<CreateCourseModel> logger, CourseRepository courseRepository)
    : PageModel
{
    private readonly ILogger<CreateCourseModel> _logger = logger;

    [BindProperty]
    [Required, Display(Name = "Course name")]
    public string Name { get; set; } = String.Empty;

    [BindProperty]
    [Required]
    [Range(0, 20)]
    public uint GroupSize { get; set; } = 4;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _logger.LogDebug("Create new course {Name} with group size {GroupSize}", Name, GroupSize);
        var course = await courseRepository.CreateCourse(Name, GroupSize);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = course.Id });
    }
}
