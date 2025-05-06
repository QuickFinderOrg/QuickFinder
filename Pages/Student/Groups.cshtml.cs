using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class GroupsModel(
    ILogger<GroupsModel> logger,
    UserManager<User> userManager,
    UserService userService,
    GroupRepository groupRepository,
    GroupMatchmakingService groupMatchmakingService
) : PageModel
{
    public List<GroupVM> Groups = [];

    [BindProperty]
    public bool AllowAnyone { get; set; } = false;

    public async Task<IActionResult> OnGetAsync()
    {
        var user =
            await userManager.GetUserAsync(HttpContext.User)
            ?? throw new Exception("User not found");
        var groups = await groupRepository.GetGroups(user);

        foreach (var g in groups)
        {
            var group_vm = new GroupVM()
            {
                Id = g.Id,
                Name = g.Name,
                CourseName = g.Course.Name,
                CourseId = g.Course.Id,
                GroupLimit = g.GroupLimit,
                AllowAnyone = g.AllowAnyone,
            };
            foreach (var member in g.Members)
            {
                if (member == null)
                {
                    logger.LogDebug("User not found");
                }
                else
                {
                    logger.LogDebug("User {}", member.UserName);
                    var name = await userService.GetName(member);
                    group_vm.Members.Add(
                        new GroupMemberVM()
                        {
                            name = name,
                            email = member.UserName ?? throw new Exception("username not found"),
                        }
                    );
                }
            }
            Groups.Add(group_vm);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLeaveAsync(Guid groupId)
    {
        var user =
            await userManager.GetUserAsync(HttpContext.User)
            ?? throw new Exception("User not found");

        var group = await groupRepository.GetGroup(groupId);
        if (group == null)
        {
            return NotFound();
        }

        group.Members.Remove(user);

        await groupRepository.RemoveGroupMembersAsync(group.Id, [user.Id]);
        // TODO: add load functions and model errors
        // TODO: don't match again with a group you left

        return Redirect("Groups");
    }

    public async Task<IActionResult> OnPostSearchAsync(
        Guid groupId,
        CancellationToken cancellationToken = default
    )
    {
        var group = await groupRepository.GetGroup(groupId);
        if (group == null)
        {
            return Page();
        }

        var result = await groupMatchmakingService.QueueForMatchmakingAsync(
            group.Id,
            group.Course.Id,
            cancellationToken
        );

        switch (result)
        {
            case AddToQueueResult.AlreadyInQueue:
                PageContext.ModelState.AddModelError(
                    string.Empty,
                    $"You are already in the matchmaking queue for {group.Course.Name}."
                );
                return Page();

            case AddToQueueResult.Failure:
                PageContext.ModelState.AddModelError(
                    string.Empty,
                    "Failed to add you to the matchmaking queue. Please try again later."
                );
                return Page();

            case AddToQueueResult.Success:
                logger.LogInformation(
                    "Group {GroupId} successfully added to matchmaking queue for course {CourseId}",
                    group.Id,
                    group.Course.Id
                );
                return Redirect(StudentRoutes.Groups());

            default:
                PageContext.ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
                return Page();
        }
    }

    public async Task<IActionResult> OnPostChangeAllowAnyoneAsync(Guid groupId)
    {
        // TODO: check authorization first e.g. member fo said group.
        await groupRepository.SetAllowAnyoneAsync(groupId, AllowAnyone);

        return Redirect(StudentRoutes.Groups());
    }

    public async Task<IActionResult> OnPostCancelSearchAsync(Guid groupId)
    {
        var group = await groupRepository.GetGroup(groupId);
        if (group == null)
        {
            return Page();
        }

        var result = await groupMatchmakingService.RemoveFromQueueAsync(group.Id, group.Course.Id);
        switch (result)
        {
            case RemoveFromQueueResult.Failure:
                PageContext.ModelState.AddModelError(
                    string.Empty,
                    "Failed to remove your group from the matchmaking queue. Please try again later or check if you have a new member."
                );
                return Page();

            case RemoveFromQueueResult.Success:
                logger.LogInformation(
                    "Group {GroupId} successfully removed from matchmaking queue for course {CourseId}",
                    group.Id,
                    group.Course.Id
                );
                return Redirect(StudentRoutes.Groups());

            default:
                PageContext.ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
                return Page();
        }
    }

    public class GroupVM
    {
        public required Guid Id;
        public required string Name;
        public List<GroupMemberVM> Members = [];
        public string CourseName = "";
        public Guid CourseId = Guid.Empty;
        public uint GroupLimit = 2;
        public bool AllowAnyone = false;
        public bool IsFull => Members.Count >= GroupLimit;
    }

    public class GroupMemberVM
    {
        public required string name;
        public required string email;
    }
}
