using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Teacher;

public class EditGroupModel(MatchmakingService matchmaking) : PageModel
{
    public Group? Group;
    public User[] Members = [];
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteMemberAsync(string userId, Guid groupId)
    {
        await LoadAsync(groupId);
        if (Group is not null)
        {
            await matchmaking.RemoveUserFromGroup(userId, Group.Id);
            if (Members.Length == 1)
            {
                return RedirectToPage(TeacherRoutes.CourseOverview());
            }
            await LoadAsync(groupId);   
        }
        return RedirectToPage(TeacherRoutes.EditGroup(), new { id = groupId });
    }

    public async Task LoadAsync(Guid id)
    {
        Group = await matchmaking.GetGroup(id);
        Members = Group.Members.ToArray();   
    }
}
