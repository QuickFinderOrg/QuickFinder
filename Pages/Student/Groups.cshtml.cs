using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student;

public class GroupsModel(ILogger<GroupsModel> logger, MatchmakingService matchmakingService, UserManager<User> userManager) : PageModel
{
    private readonly ILogger<GroupsModel> _logger = logger;

    public List<GroupVM> Groups = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(HttpContext.User) ?? throw new Exception("User not found");
        var groups = await matchmakingService.GetGroups(user);

        foreach (var g in groups)
        {
            var group_vm = new GroupVM();
            foreach (var member in g.Members)
            {
                if (member == null)
                {
                    _logger.LogDebug("User not found");
                }
                else
                {
                    _logger.LogDebug("User {}", member.UserName);
                    group_vm.Members.Add(new GroupMemberVM() { name = member.Name, email = member.UserName ?? throw new Exception("username not found") });
                }

            }
            Groups.Add(group_vm);

        }


        return Page();
    }

    public class GroupVM
    {
        public List<GroupMemberVM> Members = [];
    }

    public class GroupMemberVM
    {
        public required string name;
        public required string email;
    }
}
