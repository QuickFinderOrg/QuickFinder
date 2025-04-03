using QuickFinder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages.Student;

public class MatchingModel(ILogger<MatchingModel> logger, MatchmakingService matchmakingService, UserManager<User> userManager) : PageModel
{
    public Course[] Courses = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostFindGroupAsync(Guid courseId)
    {
        await LoadAsync();
        var course = Courses.First(c => c.Id == courseId);
        if (course == null)
        {
            PageContext.ModelState.AddModelError(string.Empty, "Course does not exist.");
            return Page();
        }
        var user = await userManager.GetUserAsync(HttpContext.User) ?? throw new Exception("User not found");
        var was_added_to_waitlist = await matchmakingService.AddToWaitlist(user, course);

        if (!was_added_to_waitlist)
        {
            PageContext.ModelState.AddModelError(string.Empty, "You are already in the waitlist for this course.");
            return Page();
        }

        logger.LogInformation("User {UserId} added to waitlist for course {CourseId}", user.Id, course.Id);

        return Redirect(StudentRoutes.Groups());
    }

    public async Task<IActionResult> OnPostJoinCourseAsync(Guid courseId)
    {
        await LoadAsync();

        var user = await userManager.GetUserAsync(HttpContext.User) ?? throw new Exception("User not found");

        var course = Courses.First(c => c.Id == courseId);
        if (course == null)
        {
            PageContext.ModelState.AddModelError(string.Empty, "Course does not exist.");
            return Page();
        }

        await matchmakingService.JoinCourse(user, course);

        return Page();
    }

    public async Task<IActionResult> OnPostLeaveCourseAsync(Guid courseId)
    {
        await LoadAsync();

        var user = await userManager.GetUserAsync(HttpContext.User) ?? throw new Exception("User not found");

        var course = Courses.First(c => c.Id == courseId);
        if (course == null)
        {
            PageContext.ModelState.AddModelError(string.Empty, "Course does not exist.");
            return Page();
        }

        await matchmakingService.LeaveCourse(user, course);

        return Page();
    }

    public async Task LoadAsync()
    {
        var user = await userManager.GetUserAsync(HttpContext.User) ?? throw new Exception("User not found");
        Courses = await matchmakingService.GetCourses(user);
    }
}
