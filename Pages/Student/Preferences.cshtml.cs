using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student
{
    public class PreferencesModel(UserManager<User> userManager) : PageModel
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
        }

        public async Task LoadAsync(User user)
        {
            Input = new InputModel
            {
                NewAvailability = user.Preferences.Availability,
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            user.Preferences.Availability = Input.NewAvailability;

            StatusMessage = "Confirmation link to change email sent. Please check your email.";
            return RedirectToPage("Matching");
        }
    }
}
