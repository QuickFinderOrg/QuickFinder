using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Pages;

public class StudentsModel(ILogger<StudentsModel> logger, ApplicationDbContext db) : PageModel
{
    private readonly ILogger<StudentsModel> _logger = logger;

    public List<Person> Students = [];
    public List<Group> Groups = [];

    public async void OnGet()
    {
        Students = await db.People.ToListAsync();
        Groups = await db.Groups.Include(c => c.Members).ToListAsync();
    }
}


