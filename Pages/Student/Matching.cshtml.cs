using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class MatchingModel(
    ILogger<MatchingModel> logger,
    UserManager<User> userManager,
    DiscordService discordService,
    IOptions<DiscordServiceOptions> options,
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
                return Redirect(StudentRoutes.Groups());

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

        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);

        var discordIdClaim =
            c.Find(c => c.Type == "DiscordId") ?? throw new Exception("DiscordId claim not found");
        var discordTokenClaim =
            c.Find(c => c.Type == "DiscordToken")
            ?? throw new Exception("DiscordId claim not found");

        var server = await discordService.GetCourseServer(course.Id);
        if (server.Length == 0)
        {
            await discordService.InviteToServer(
                ulong.Parse(discordIdClaim.Value),
                discordTokenClaim.Value,
                ulong.Parse(options.Value.ServerId)
            );
        }
        else
        {
            await discordService.InviteToServer(
                ulong.Parse(discordIdClaim.Value),
                discordTokenClaim.Value,
                server[0].Id
            );
        }

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
