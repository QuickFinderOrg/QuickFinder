using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Teacher;

public class EditGroupModel(GroupRepository groupRepository, UserService userService) : PageModel
{
    public Group? Group;
    public User[] Members = [];

    public async Task<IActionResult> OnGetAsync(Guid groupId)
    {
        await LoadAsync(groupId);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteMemberAsync(string userId, Guid groupId)
    {
        await LoadAsync(groupId);
        if (Group is not null)
        {
            var user = await userService.GetUser(userId);
            if (user == null)
            {
                return NotFound();
            }

            Group.Members.Remove(user);
            await groupRepository.UpdateAsync(Group);
            if (Members.Length == 1)
            {
                return RedirectToPage(TeacherRoutes.CourseOverview());
            }
            await LoadAsync(groupId);
        }
        return RedirectToPage(TeacherRoutes.EditGroup(), new { groupId });
    }

    public async Task LoadAsync(Guid id)
    {
        Group = await groupRepository.GetGroup(id);
        Members = Group.Members.ToArray();
    }
}
