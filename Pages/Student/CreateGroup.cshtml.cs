using group_finder.Domain.Matchmaking;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student;

public class CreateGroupModel(MatchmakingService matchmaking, UserManager<User> userManager, IMediator mediator) : PageModel
{
    [BindProperty]
    public Course Course { get; set; } = default!;
    public Group[] Groups = [];
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id != Guid.Empty)
        {
            await LoadAsync(id);   
        }
        return Page();
    }

    public async Task<IActionResult> OnPostJoinGroupAsync(Guid groupId)
    {
        var events = new List<object>();
        var user = await userManager.GetUserAsync(User);
        var group = await matchmaking.GetGroup(groupId);
        if (user is not null)
        {
            await matchmaking.AddToGroup(user, group, events);
        }

        foreach (var e in events)
        {
            await mediator.Publish(e);
        }
        
        return RedirectToPage(StudentRoutes.CreateGroup(), new { id = Course.Id});
    }

    public async Task LoadAsync(Guid id)
    {
        Course = await matchmaking.GetCourse(id);
        Groups = await matchmaking.GetAvailableGroups(id);
    }
}