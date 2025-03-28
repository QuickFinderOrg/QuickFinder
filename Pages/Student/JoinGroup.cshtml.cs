using QuickFinder.Domain.Matchmaking;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages.Student;

public class JoinGroupModel(MatchmakingService matchmaking, UserManager<User> userManager) : PageModel
{
    [BindProperty]
    public Course Course { get; set; } = default!;
    public Group[] Groups = [];
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var user = await userManager.GetUserAsync(User) ?? throw new Exception("User not found");
        if (id != Guid.Empty)
        {
            await LoadAsync(id);
        }
        if (await matchmaking.IsUserInGroup(user, Course))
        {
            return RedirectToPage(StudentRoutes.Groups());
        }
        return Page();
    }

    public async Task<IActionResult> OnPostJoinGroupAsync(Guid groupId)
    {
        var user = await userManager.GetUserAsync(User);
        var group = await matchmaking.GetGroup(groupId);
        if (user is not null)
        {
            await matchmaking.AddToGroup(user, group);
        }

        return RedirectToPage(StudentRoutes.JoinGroup(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostLeaveGroupAsync(Guid groupId)
    {
        var user = await userManager.GetUserAsync(User) ?? throw new Exception("User not found");
        var group = await matchmaking.GetGroup(groupId);
        await matchmaking.RemoveUserFromGroup(user.Id, group.Id);
        return RedirectToPage(StudentRoutes.JoinGroup(), new { id = Course.Id });
    }

    public IActionResult OnPostCreateGroup()
    {
        return RedirectToPage(StudentRoutes.CreateGroup(), new { courseId = Course.Id });
    }

    public async Task LoadAsync(Guid id)
    {
        Course = await matchmaking.GetCourse(id);
        Groups = await matchmaking.GetAvailableGroups(id);
    }
}