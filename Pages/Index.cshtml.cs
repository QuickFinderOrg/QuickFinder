using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages;

[Authorize(Policy = "Student")]
public class IndexModel(
    ILogger<IndexModel> logger,
    UserManager<User> userManager,
    UserService userService
) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    public string? Name { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user =
            await userManager.GetUserAsync(HttpContext.User)
            ?? throw new Exception("User not found");

        var name = await userService.GetName(user);
        if (!string.IsNullOrEmpty(name))
        {
            Name = name;
        }

        return Page();
    }
}
