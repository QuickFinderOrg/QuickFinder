using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student;

public class GroupsModel(ILogger<GroupsModel> logger, MatchmakingService matchmakingService, UserManager<User> userManager, UserService userService) : PageModel
{
    private readonly ILogger<GroupsModel> _logger = logger;

    public List<GroupVM> Groups = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(HttpContext.User) ?? throw new Exception("User not found");
        var groups = await matchmakingService.GetGroups(user);

        foreach (var g in groups)
        {
            var group_vm = new GroupVM() { Id = g.Id.ToString(), Course = g.Course.Name, GroupLimit = g.GroupLimit };
            foreach (var member in g.Members)
            {
                if (member == null)
                {
                    _logger.LogDebug("User not found");
                }
                else
                {
                    _logger.LogDebug("User {}", member.UserName);
                    var name = await userService.GetName(member);
                    group_vm.Members.Add(new GroupMemberVM() { name = name, email = member.UserName ?? throw new Exception("username not found") });
                }

            }
            Groups.Add(group_vm);

        }


        return Page();
    }

    public async Task<IActionResult> OnPostLeaveAsync(Guid groupId)
    {
        var user = await userManager.GetUserAsync(HttpContext.User) ?? throw new Exception("User not found");
        await matchmakingService.RemoveUserFromGroup(user.Id, groupId);
        // TODO: add load functions and model errors
        // TODO: don't match again with a group you left

        return Redirect("Groups");

    }

    public class GroupVM
    {
        public required string Id;
        public List<GroupMemberVM> Members = [];
        public string Course = "";
        public uint GroupLimit = 2;
    }

    public class GroupMemberVM
    {
        public required string name;
        public required string email;
    }
}
