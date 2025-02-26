using System.Security.Claims;
using System.Threading.Tasks;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student;

public class MatchingModel(ILogger<MatchingModel> logger, MatchmakingService matchmakingService, UserManager<User> userManager) : PageModel
{
    private readonly ILogger<MatchingModel> _logger = logger;
    public Course[] Courses = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
        Courses = await matchmakingService.GetCourses();
    }

    public async Task<IActionResult> OnPostAsync(string CourseId)
    {
        var course_guid = Guid.Parse(CourseId);
        await LoadAsync();
        var course = Courses.First(c => c.Id == course_guid);
        if (course == null)
        {
            return Page();
        }
        var user = await userManager.GetUserAsync(HttpContext.User) ?? throw new Exception("User not found");
        await matchmakingService.AddToWaitlist(user, course);
        await matchmakingService.DoMatching();

        return Redirect(StudentRoutes.Groups());
    }

    public async Task LoadAsync()
    {
        Courses = await matchmakingService.GetCourses();
    }
}
