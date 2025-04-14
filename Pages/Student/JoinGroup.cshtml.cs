using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class JoinGroupModel(
    UserManager<User> userManager,
    CourseRepository courseRepository,
    GroupRepository groupRepository
) : PageModel
{
    [BindProperty]
    public Course Course { get; set; } = default!;
    public Group[] Groups = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var user = await userManager.GetUserAsync(User);

        if (user == null)
        {
            return NotFound();
        }

        var course = await courseRepository.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound();
        }
        Course = course;

        var groups = await groupRepository.GetAvailableGroups(id);
        if (groups == null)
        {
            return NotFound();
        }

        if (await groupRepository.IsUserInGroup(user, Course))
        {
            return RedirectToPage(StudentRoutes.Groups());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostJoinGroupAsync(Guid groupId)
    {
        var user = await userManager.GetUserAsync(User);
        var group = await groupRepository.GetGroup(groupId);
        if (user is not null)
        {
            await groupRepository.AddToGroup(user, group);
        }

        return RedirectToPage(StudentRoutes.JoinGroup(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostLeaveGroupAsync(Guid groupId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var group = await groupRepository.GetGroup(groupId);
        if (group == null)
        {
            return NotFound();
        }

        group.Members.Remove(user);
        await groupRepository.UpdateAsync(group);

        return RedirectToPage(StudentRoutes.JoinGroup(), new { id = Course.Id });
    }

    public IActionResult OnPostCreateGroup()
    {
        return RedirectToPage(StudentRoutes.CreateGroup(), new { courseId = Course.Id });
    }
}
