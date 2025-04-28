using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class MatchingModel(
    ILogger<MatchingModel> logger,
    UserManager<User> userManager,
    MatchmakingService matchmakingService,
    CourseRepository courseRepository,
    GroupRepository groupRepository,
    PreferencesRepository preferencesRepository
) : PageModel
{
    public Course[] Courses = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostFindGroupAsync(
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        await LoadAsync();
        var course = Courses.First(c => c.Id == courseId);
        if (course == null)
        {
            PageContext.ModelState.AddModelError(string.Empty, "Course does not exist.");
            return Page();
        }
        var user =
            await userManager.GetUserAsync(HttpContext.User)
            ?? throw new Exception("User not found");
        if (await groupRepository.CheckIfInGroup(user, course))
        {
            PageContext.ModelState.AddModelError(
                string.Empty,
                "You are already in a group for this course."
            );
            return Page();
        }

        var result = await matchmakingService.QueueForMatchmakingAsync(
            user.Id,
            course.Id,
            cancellationToken
        );

        switch (result)
        {
            case AddToQueueResult.AlreadyInQueue:
                PageContext.ModelState.AddModelError(
                    string.Empty,
                    $"You are already in the matchmaking queue for {course.Name}."
                );
                return Page();

            case AddToQueueResult.Failure:
                PageContext.ModelState.AddModelError(
                    string.Empty,
                    "Failed to add you to the matchmaking queue. Please try again later."
                );
                return Page();

            case AddToQueueResult.Success:
                logger.LogInformation(
                    "User {UserId} successfully added to matchmaking queue for course {CourseId}",
                    user.Id,
                    course.Id
                );
                await LoadAsync();
                return Page();

            default:
                PageContext.ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
                await LoadAsync();
                return Page();
        }
    }

    public async Task<IActionResult> OnPostLeaveQueue(
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        await LoadAsync();
        var course = Courses.First(c => c.Id == courseId);
        var user =
            await userManager.GetUserAsync(HttpContext.User)
            ?? throw new Exception("User not found");
        var result = await matchmakingService.RemoveFromQueueAsync(
            user.Id,
            course.Id,
            cancellationToken
        );
        switch (result)
        {
            case RemoveFromQueueResult.Failure:
                PageContext.ModelState.AddModelError(
                    string.Empty,
                    "Failed to remove you from the matchmaking queue. Please try again later or check if you are in a group."
                );
                return Page();

            case RemoveFromQueueResult.Success:
                PageContext.ViewData["SuccessMessage"] =
                    "You have successfully been removed from the matchmaking queue.";
                logger.LogInformation(
                    "User {UserId} successfully removed from matchmaking queue for course {CourseId}",
                    user.Id,
                    course.Id
                );
                await LoadAsync();
                return Page();

            default:
                PageContext.ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
                await LoadAsync();
                return Page();
        }
    }

    public async Task<IActionResult> OnPostJoinCourseAsync(Guid courseId)
    {
        await LoadAsync();

        var user =
            await userManager.GetUserAsync(HttpContext.User)
            ?? throw new Exception("User not found");

        var course = Courses.First(c => c.Id == courseId);
        if (course == null)
        {
            PageContext.ModelState.AddModelError(string.Empty, "Course does not exist.");
            return Page();
        }

        await courseRepository.JoinCourse(user, course);

        if (await preferencesRepository.GetCoursePreferences(course.Id, user.Id) is null)
        {
            return RedirectToPage(
                StudentRoutes.CoursePreferences(),
                new { courseId = course.Id, returnUrl = StudentRoutes.Matching() }
            );
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLeaveCourseAsync(Guid courseId)
    {
        await LoadAsync();

        var user =
            await userManager.GetUserAsync(HttpContext.User)
            ?? throw new Exception("User not found");

        var course = Courses.First(c => c.Id == courseId);
        if (course == null)
        {
            PageContext.ModelState.AddModelError(string.Empty, "Course does not exist.");
            return Page();
        }

        await courseRepository.LeaveCourse(user, course);

        return Page();
    }

    public async Task LoadAsync()
    {
        var user =
            await userManager.GetUserAsync(HttpContext.User)
            ?? throw new Exception("User not found");
        Courses = await courseRepository.GetAllAsync();
    }
}
