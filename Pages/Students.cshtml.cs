using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Pages;

public class StudentsModel(ILogger<StudentsModel> logger, ApplicationDbContext db, MatchmakingService matchmaking) : PageModel
{
    private readonly ILogger<StudentsModel> _logger = logger;

    public List<WaitingPerson> Students = [];
    public List<Group> Groups = [];

    public async Task OnGet()
    {
        Students = await db.WaitingPeople.Include(c => c.Person).ToListAsync();
        Groups = await db.Groups.Include(c => c.Members).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await matchmaking.DoMatching();
        Students = await db.WaitingPeople.Include(c => c.Person).ToListAsync();
        Groups = await db.Groups.Include(c => c.Members).ToListAsync();
        return Page();
    }
}


