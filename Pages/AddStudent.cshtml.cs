using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class AddStudentsModel(ILogger<AddStudentsModel> logger, ApplicationDbContext db, UserService userService, MatchmakingService matchmakingService) : PageModel
{
    private readonly ILogger<AddStudentsModel> _logger = logger;

    public async Task<IActionResult> OnPostAsync(string name, Availability availability)
    {
        _logger.LogDebug(" Add {name} {availability}", name, availability);
        Course course = new() {Name = "Course"};
        var user = await userService.CreateUser("Test@mail.com", name, "Lego1!");
        await matchmakingService.AddToWaitlist(user, course);
        await db.SaveChangesAsync();
        return RedirectToPage("Students");
    }
}


