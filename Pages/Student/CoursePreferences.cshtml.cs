using System.ComponentModel.DataAnnotations;
using QuickFinder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuickFinder.Pages.Student;

public class CoursePreferencesModel(MatchmakingService matchmaking, UserManager<User> userManager, ILogger<CoursePreferences> logger) : PageModel
{
    [TempData]
    public string StatusMessage { get; set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    [BindProperty]
    public Guid CourseId { get; set; }

    [BindProperty]
    public DaysOfTheWeek Days { get; set; }

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

        [Display(Name = "Monday")]
        public bool Monday { get; set; } = false;

        [Display(Name = "Tuesday")]
        public bool Tuesday { get; set; } = false;

        [Display(Name = "Wednesday")]
        public bool Wednesday { get; set; } = false;

        [Display(Name = "Thursday")]
        public bool Thursday { get; set; } = false;

        [Display(Name = "Friday")]
        public bool Friday { get; set; } = false;

        [Display(Name = "Saturday")]
        public bool Saturday { get; set; } = false;

        [Display(Name = "Sunday")]
        public bool Sunday { get; set; } = false;

    }

    public CoursePreferences CoursePreferences { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid courseId)
    {
        Console.WriteLine($"CourseId {courseId}");
        if (Guid.Empty == courseId)
        {
            return NotFound("CourseId must not be empty");
        }

        var course = await matchmaking.GetCourse(courseId);
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
        return Page();
    }

    public async Task LoadAsync(Guid courseId)
    {
        var userId = userManager.GetUserId(User);
        CourseId = courseId;
        var coursePreferences = await matchmaking.GetCoursePreferences(courseId, userId);
        CoursePreferences = coursePreferences;
        Input = new InputModel
        {
            NewAvailability = Availability.Afternoons,
            GroupSize = 2,
            Monday = coursePreferences.Days.HasDay(DaysOfTheWeek.Monday),
            Tuesday = coursePreferences.Days.HasDay(DaysOfTheWeek.Tuesday),
            Wednesday = coursePreferences.Days.HasDay(DaysOfTheWeek.Wednesday),
            Thursday = coursePreferences.Days.HasDay(DaysOfTheWeek.Thursday),
            Friday = coursePreferences.Days.HasDay(DaysOfTheWeek.Friday),
            Saturday = coursePreferences.Days.HasDay(DaysOfTheWeek.Saturday),
            Sunday = coursePreferences.Days.HasDay(DaysOfTheWeek.Sunday)
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
            return NotFound("CourseId is empty");
        }

        await LoadAsync(courseId);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = userManager.GetUserId(User);
        CourseId = courseId;
        var coursePreferences = await matchmaking.GetCoursePreferences(courseId, userId);
        // var newDays = DaysOfTheWeek.None.SetFromArray([Input.Monday, Input.Tuesday, Input.Wednesday, Input.Thursday, Input.Friday, Input.Saturday, Input.Sunday]);
        var newDays = DaysOfTheWeek.None;
        if (Input.Monday)
        {
            newDays = newDays.WithDay(DaysOfTheWeek.Monday);
        }
        logger.LogInformation("Monday: {monday}", Input);
        logger.LogInformation("nDays: {days}", newDays);
        logger.LogInformation("Days of: {days}", Days);
        coursePreferences.Days = Days;

        await matchmaking.UpdateCoursePreferencesAsync(courseId, "user", coursePreferences);

        return RedirectToPage(new { courseId });

    }
}