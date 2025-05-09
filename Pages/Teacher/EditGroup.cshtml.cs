using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Teacher;

public class EditGroupModel(GroupRepository groupRepository, UserService userService) : PageModel
{
    public Group? Group;
    public User[] Members = [];

    public Guid CourseId;

    public async Task<IActionResult> OnGetAsync(Guid groupId, Guid courseId)
    {
        if (groupId == Guid.Empty || courseId == Guid.Empty)
        {
            return RedirectToPage(TeacherRoutes.CourseOverview());
        }
        CourseId = courseId;
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

            await groupRepository.RemoveGroupMembersAsync(Group.Id, [userId]);
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
