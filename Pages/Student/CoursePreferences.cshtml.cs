using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student;

public class CoursePreferencesModel(
    UserManager<User> userManager,
    ILogger<CoursePreferences> logger,
    CourseRepository courseRepository,
    PreferencesRepository preferencesRepository
) : PageModel
{
    [TempData]
    public string StatusMessage { get; set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    [BindProperty]
    public Guid CourseId { get; set; }

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Availability")]
        public Availability NewAvailability { get; set; }

        public LanguageFlags SpokenLanguages { get; set; }

        [Required]
        [Display(Name = "Languages")]
        public LanguageFlags SelectedLanguages { get; set; }

        public DaysOfTheWeek Days { get; set; }

        public StudyLocation StudyLocation { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid courseId, string returnUrl)
    {
        ReturnUrl = returnUrl;
        Console.WriteLine($"CourseId {courseId}");
        if (Guid.Empty == courseId)
        {
            return NotFound("CourseId must not be empty");
        }

        var course = await courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound("Course not found");
        }
        Console.WriteLine($"Course {course}");

        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        await LoadAsync(courseId);
        Input.SpokenLanguages = user.Preferences.Language;
        return Page();
    }

    public async Task LoadAsync(Guid courseId)
    {
        var userId = userManager.GetUserId(User) ?? throw new Exception("User not found");
        var user = await userManager.GetUserAsync(User) ?? throw new Exception("User not found");
        CourseId = courseId;

        var coursePreferences = await preferencesRepository.GetCoursePreferences(courseId, userId);
        if (coursePreferences is null)
        {
            await preferencesRepository.CreateNewCoursePreferences(courseId, userId);
            Input = new InputModel
            {
                NewAvailability = user.Preferences.GlobalAvailability,
                Days = user.Preferences.GlobalDays,
            };
        }
        else
        {
            Input = new InputModel
            {
                NewAvailability = coursePreferences.Availability,
                Days = coursePreferences.Days,
                StudyLocation = coursePreferences.StudyLocation,
            };
        }
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
            return NotFound("CourseId is empty");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = userManager.GetUserId(User) ?? throw new Exception("User not found");
        CourseId = courseId;
        var coursePreferences =
            await preferencesRepository.GetCoursePreferences(courseId, userId)
            ?? throw new Exception("Preferences not found");

        logger.LogInformation("Days of: {days}", Input.Days);
        coursePreferences.Availability = Input.NewAvailability;
        coursePreferences.Days = Input.Days;
        coursePreferences.StudyLocation = Input.StudyLocation;
        user.Preferences.Language = Input.SelectedLanguages;

        await preferencesRepository.UpdateCoursePreferencesAsync(
            courseId,
            "user",
            coursePreferences
        );

        return RedirectToPage(ReturnUrl, new { courseId });
    }
}
