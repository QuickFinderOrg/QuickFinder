using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Teacher;

public class CreateCourseModel(ILogger<CourseGroupsModel> logger, MatchmakingService matchmakingService) : PageModel
{
    private readonly ILogger<CourseGroupsModel> _logger = logger;
    
    [BindProperty]
    public required string Name { get; set; }

    [BindProperty]
    public required uint GroupSize { get; set; }
    
    [BindProperty]
    public bool AllowCustomSize { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogDebug("Create new course {Name} with group size {GroupSize} and allow custom size {AllowCustomSize}", Name, GroupSize, AllowCustomSize);
        await matchmakingService.CreateCourse(Name, GroupSize, AllowCustomSize);
        return RedirectToPage();
    }
}