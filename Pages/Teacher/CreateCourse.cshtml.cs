using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Teacher;

public class CreateCourseModel(ILogger<CourseGroupsModel> logger) : PageModel
{
    private readonly ILogger<CourseGroupsModel> _logger = logger;

    public async Task<IActionResult> OnGetAsync()
    {
        return Page();
    }
}