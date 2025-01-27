using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Pages;

public class StudentsModel(ILogger<StudentsModel> logger, ApplicationDbContext db, MatchmakingService matchmaking, UserService userService) : PageModel
{
    private readonly ILogger<StudentsModel> _logger = logger;

    public List<User> Students = [];
    public List<Group> Groups = [];

    public async Task OnGet()
    {
        var waitlist = await db.People.Include(o => o.User).ToListAsync();
        foreach (Person person in waitlist)
        {
            Students.Add(person.User);
        }
        Groups = await db.Groups.ToListAsync();
    }

    public async Task<IActionResult> OnPostMatchAsync()
    {
        await matchmaking.DoMatching();
        return RedirectToPage("Students");

    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        await matchmaking.Reset();
        var users = await userService.GetAllUsers();
        foreach (var user in users)
        {
            await matchmaking.AddToWaitlist(user);
        }

        return RedirectToPage("Students");
    }
}


