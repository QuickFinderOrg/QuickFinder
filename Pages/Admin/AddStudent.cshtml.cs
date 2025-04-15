using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Data;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Admin;

public class AddStudentsModel(
    ILogger<AddStudentsModel> logger,
    ApplicationDbContext db,
    UserService userService,
    MatchmakingService matchmakingService,
    CourseRepository courseRepository
) : PageModel
{
    private readonly ILogger<AddStudentsModel> _logger = logger;

    public async Task<IActionResult> OnPostAsync(
        string name,
        Availability availability,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug(" Add {name} {availability}", name, availability);
        var courses = await courseRepository.GetAllAsync(cancellationToken);
        var user = await userService.CreateUser(Guid.NewGuid() + "@example.com", name, "Lego1!");
        await matchmakingService.QueueForMatchmakingAsync(
            user.Id,
            courses[0].Id,
            cancellationToken
        );
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToPage(AdminRoutes.Students());
    }
}
