using System.ComponentModel.DataAnnotations;
using QuickFinder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages.Student;

public class CreateGroupModel(
    MatchmakingService matchmaking,
    UserManager<User> userManager,
    CourseRepository courseRepository
    ) : PageModel
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
            Course = await courseRepository.GetCourse(courseId);
        }

        if (await matchmaking.IsUserInGroup(user, Course))
        {
            return RedirectToPage(StudentRoutes.JoinGroup(), new { id = Course.Id });
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task LoadAsync(User user)
    {
        Input = new InputModel
        {
            NewAvailability = Availability.Afternoons,
            GroupSize = 2,
            SpokenLanguages = user.Preferences.Language
        };
        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync(Guid courseId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (courseId != Guid.Empty)
        {
            Course = await courseRepository.GetCourse(courseId);
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var coursePreferences = new CoursePreferences() { User = user, Course = Course, Availability = Input.NewAvailability, GroupSize = Input.GroupSize };
        var userPreferences = new UserPreferences() { Language = Input.SelectedLanguages };
        var groupPreferences = Preferences.From(userPreferences, coursePreferences);

        await matchmaking.CreateGroup(user, Course, groupPreferences);

        return RedirectToPage(StudentRoutes.JoinGroup(), new { id = Course.Id });
    }
}