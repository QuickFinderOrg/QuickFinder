using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Pages;

public class StudentsModel(ILogger<StudentsModel> logger, ApplicationDbContext db, MatchmakingService matchmaking, UserService userService) : PageModel
{
    private readonly ILogger<StudentsModel> _logger = logger;

    public List<Ticket> Students = [];
    public List<Group> Groups = [];

    public async Task OnGet()
    {
        var waitlist = await db.Tickets.Include(p => p.User).Include(p => p.Course).ToListAsync();
        foreach (Ticket ticket in waitlist)
        {
            Students.Add(ticket);
        }
        Groups = await db.Groups.Include(g => g.Members).Include(g => g.Course).ToListAsync();
    }

    public async Task<IActionResult> OnPostMatchAsync()
    {
        await matchmaking.DoMatching();
        return RedirectToPage("Students");

    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        await matchmaking.Reset();
        var courses = await matchmaking.GetCourses();
        var course = courses[0];
        var users = await userService.GetAllUsers();
        foreach (var user in users)
        {
            await matchmaking.AddToWaitlist(user, course);
        }

        return RedirectToPage("Students");
    }
}


