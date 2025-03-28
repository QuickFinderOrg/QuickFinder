using System.ComponentModel.DataAnnotations;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student;

public class CreateGroupModel(MatchmakingService matchmaking, UserManager<User> userManager) : PageModel
{
    [TempData]
    public string StatusMessage { get; set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public class InputModel
    {
        [Required]
        [Display(Name = "Availability")]
        public Availability NewAvailability { get; set; }

        [Required]
        [Display(Name = "Group Size")]
        public uint GroupSize { get; set; }

        public Languages[] SpokenLanguages { get; set; } = [];

        [Required]
        [Display(Name = "Languages")] 
        public Languages[] SelectedLanguages { get; set; } = [];

    }

    [BindProperty]
    public Course Course { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid courseId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (courseId != Guid.Empty)
        {
            Course = await matchmaking.GetCourse(courseId);
        }

        if (await matchmaking.IsUserInGroup(user, Course))
        {
            return RedirectToPage(StudentRoutes.JoinGroup(), new { id = Course.Id});
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task LoadAsync(User user)
    {
        Input = new InputModel
        {
            NewAvailability = user.Preferences.Availability,
            GroupSize = user.Preferences.GroupSize,
            SpokenLanguages = user.Preferences.Language
        };
        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        Course = await matchmaking.GetCourse(Course.Id);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }


        await matchmaking.CreateGroup(user, Course);

        return RedirectToPage(StudentRoutes.JoinGroup(), new { id = Course.Id});
    }
}