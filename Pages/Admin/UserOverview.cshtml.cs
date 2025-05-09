using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages.Admin;

public class UserOverviewModel(
    ILogger<UserOverviewModel> logger,
    UserService userService,
    UserManager<User> userManager,
    IAuthorizationService authorizationService
) : PageModel
{
    private readonly ILogger<UserOverviewModel> _logger = logger;

    public List<User> Users = [];
    public List<User> Teachers = [];

    [BindProperty]
    public string? SearchQuery { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task OnPostMakeTeacherAsync(string userId)
    {
        var user = await userService.GetUser(userId);
        await userManager.AddClaimAsync(
            user,
            new Claim(ApplicationClaimTypes.IsTeacher, true.ToString())
        );
        await LoadAsync();
    }

    public async Task OnPostRemoveTeacherAsync(string userId)
    {
        var user = await userService.GetUser(userId);
        await userManager.RemoveClaimAsync(
            user,
            new Claim(ApplicationClaimTypes.IsTeacher, true.ToString())
        );
        await LoadAsync();
    }

    public async Task OnPostSearchAsync()
    {
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        var users = await userService.GetAllUsers();

        foreach (User user in users)
        {
            if (
                !string.IsNullOrEmpty(SearchQuery)
                && user.UserName != null
                && !user.UserName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                && !(await userService.GetName(user)).Contains(
                    SearchQuery,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            var claims = await userManager.GetClaimsAsync(user);
            var isTeacher = claims.Any(c =>
                c.Type == ApplicationClaimTypes.IsTeacher && c.Value == true.ToString()
            );
            if (isTeacher)
            {
                Teachers.Add(user);
            }
            else
            {
                Users.Add(user);
            }
        }
        _logger.LogInformation("LoadAsync");
    }
}
