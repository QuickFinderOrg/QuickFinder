using QuickFinder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.DiscordDomain;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace QuickFinder.Pages.Student;

public class MatchingModel(
    ILogger<MatchingModel> logger,
    MatchmakingService matchmakingService,
    UserManager<User> userManager,
    DiscordService discordService,
    IOptions<DiscordServiceOptions> options,
    TicketRepository ticketRepository,
    CourseRepository courseRepository
    ) : PageModel
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
        if (await matchmakingService.CheckIfInGroup(user, course))
        {
            PageContext.ModelState.AddModelError(string.Empty, "You are already in a group for this course.");
            return Page();
        }

        var was_added_to_waitlist = await ticketRepository.AddToWaitlist(user, course);

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

        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);

        var discordIdClaim = c.Find(c => c.Type == "DiscordId") ?? throw new Exception("DiscordId claim not found");
        var discordTokenClaim = c.Find(c => c.Type == "DiscordToken") ?? throw new Exception("DiscordId claim not found");

        var server = await discordService.GetCourseServer(course.Id);
        if (server.Length == 0)
        {
            await discordService.InviteToServer(ulong.Parse(discordIdClaim.Value), discordTokenClaim.Value, ulong.Parse(options.Value.ServerId));
        }
        else
        {
            await discordService.InviteToServer(ulong.Parse(discordIdClaim.Value), discordTokenClaim.Value, server[0].Id);
        }

        if (await matchmakingService.GetCoursePreferences(course.Id, user.Id) is null)
        {
            return RedirectToPage(StudentRoutes.CoursePreferences(), new { courseId = course.Id, returnUrl = StudentRoutes.Matching() });
        }

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
        Courses = await courseRepository.GetAllAsync();
    }
}
