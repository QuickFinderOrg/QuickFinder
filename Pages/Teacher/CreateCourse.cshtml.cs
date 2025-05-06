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
    [Range(2, 20)]
    public uint GroupSize { get; set; } = 2;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _logger.LogDebug("Create new course {Name} with group size {GroupSize}", Name, GroupSize);
        var course = new Course() { Name = Name, GroupSize = GroupSize };
        try
        {
            await courseRepository.AddAsync(course);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create course {Name}", Name);
            ViewData["ErrorMessage"] = e.Message;
            return Page();
        }

        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = course.Id });
    }
}
