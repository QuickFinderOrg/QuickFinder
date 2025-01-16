using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class AddStudentsModel(ILogger<AddStudentsModel> logger, ApplicationDbContext db) : PageModel
{
    private readonly ILogger<AddStudentsModel> _logger = logger;

    public async Task<IActionResult> OnPostAsync(string name, Availability availability)
    {
        _logger.LogDebug(" Add {name} {availability}", name, availability);
        var person = new Person() { UserId = Guid.NewGuid(), Name = name, Criteria = new Criteria() { Availability = availability, Language = "en" }, Preferences = new Preferences() };
        db.Add(person);
        db.Add(new WaitingPerson() { Person = person });
        await db.SaveChangesAsync();
        return RedirectToPage("Students");
    }
}


