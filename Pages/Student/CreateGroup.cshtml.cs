using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student;

public class CreateGroupModel(MatchmakingService matchmaking) : PageModel
{
    public Course? Course;
    public Group[] Groups = [];
    public async Task<IActionResult> OnGetAsync(Guid Id)
    {
        if (Id != Guid.Empty)
        {
            await LoadAsync(Id);   
        }
        return Page();
    }

    public async Task LoadAsync(Guid Id)
    {
        Course = await matchmaking.GetCourse(Id);
        Groups = await matchmaking.GetAvailableGroups(Id);
    }
}