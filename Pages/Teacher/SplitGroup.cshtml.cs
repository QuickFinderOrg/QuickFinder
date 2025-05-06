using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Teacher;

public class SplitGroupModel(
    GroupRepository groupRepository,
    CourseRepository courseRepository,
    UserManager<User> userManager
) : PageModel
{
    public Group Group { get; set; } = default!;

    public User[] Members { get; set; } = [];

    [BindProperty]
    public string[] SelectedMembers { get; set; } = [];
    public List<User> NewGroupMembers { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid groupId)
    {
        await LoadAsync(groupId);
        return Page();
    }

    public async Task<IActionResult> OnPostSplitAsync(
        Guid groupId,
        CancellationToken cancellationToken = default
    )
    {
        await LoadAsync(groupId);
        // TODO: improve loop logic. maybe move into service
        foreach (var userId in SelectedMembers)
        {
            var user =
                await userManager.FindByIdAsync(userId) ?? throw new Exception("User not found");
            await groupRepository.RemoveGroupMembersAsync(Group.Id, [userId], cancellationToken);
            NewGroupMembers.Add(user);
        }
        var course = await courseRepository.GetByIdAsync(Group.Course.Id);
        if (course == null)
        {
            PageContext.ViewData["ErrorMessage"] = "Unexpected error, please try again.";
            return Page();
        }

        var newGroup = new Group
        {
            Course = Group.Course,
            Preferences = Group.Preferences,
            GroupLimit = Group.GroupLimit,
            IsComplete = true,
        };
        newGroup.Members.AddRange(NewGroupMembers);
        await groupRepository.AddAsync(newGroup, cancellationToken);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = course.Id });
    }

    public async Task LoadAsync(Guid id)
    {
        Group = await groupRepository.GetGroup(id);
        Members = [.. Group.Members];
    }
}
