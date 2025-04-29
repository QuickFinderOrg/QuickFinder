using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class CreateGroupModel(
    UserManager<User> userManager,
    CourseRepository courseRepository,
    GroupRepository groupRepository
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

        [Display(Name = "Allow anyone to join?")]
        public bool AllowAnyone { get; set; }
        public LanguageFlags SpokenLanguages { get; set; }

        [Required]
        [Display(Name = "Languages")]
        public LanguageFlags SelectedLanguages { get; set; }
    }

    public Course Course { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid courseId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (courseId == Guid.Empty)
        {
            return NotFound();
        }

        var course = await courseRepository.GetByIdAsync(courseId);

        if (course == null)
        {
            return NotFound();
        }

        if (await groupRepository.IsUserInGroup(user, Course))
        {
            return RedirectToPage(StudentRoutes.CourseOverview(), new { courseId = Course.Id });
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task LoadAsync(User user)
    {
        Input = new InputModel
        {
            NewAvailability = Availability.Afternoons,
            SpokenLanguages = user.Preferences.Language,
            AllowAnyone = false,
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

        if (courseId == Guid.Empty)
        {
            return NotFound();
        }

        var course = await courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }
        Course = course;

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var coursePreferences = new CoursePreferences()
        {
            User = user,
            Course = Course,
            Availability = Input.NewAvailability,
        };
        var userPreferences = new UserPreferences() { Language = Input.SelectedLanguages };
        var groupPreferences = Preferences.From(userPreferences, coursePreferences);
        var group = new Group()
        {
            Course = Course,
            Preferences = groupPreferences,
            AllowAnyone = Input.AllowAnyone,
            GroupLimit = Course.GroupSize,
        };
        group.Members.Add(user);

        await groupRepository.AddAsync(group);

        return RedirectToPage(StudentRoutes.CourseOverview(), new { courseId = Course.Id });
    }
}
