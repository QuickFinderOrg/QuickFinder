using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Admin;

public class UserOverviewModel(UserManager<User> userManager, ILogger<StudentsModel> logger, UserService userService, AdminService adminService) : PageModel
{
    private readonly ILogger<StudentsModel> _logger = logger;

    public List<User> Users = [];
    public List<User> Teachers = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMakeTeacherAsync(string userId)
    {
        var user = await userService.GetUser(userId);
        await adminService.MakeTeacher(user);
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveTeacherAsync(string userId)
    {
        var user = await userService.GetUser(userId);
        await adminService.RemoveTeacher(user);
        await LoadAsync();
        return Page();
    }
    public async Task LoadAsync()
    {
        var users = await userService.GetAllUsers();
        
        foreach (User user in users)
        {
            var claims = await userManager.GetClaimsAsync(user);
            var c = new List<Claim>(claims);
            var isTeacher = c.Find(c => c.Type == "IsTeacher");
            if(isTeacher is not null && isTeacher.Value is not null)
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


