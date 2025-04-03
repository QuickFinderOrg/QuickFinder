using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Student
{
    public class PreferencesModel(UserManager<User> userManager) : PageModel
    {
        [TempData]
        public string StatusMessage { get; set; } = null!;

        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public class InputModel
        {
            public Languages[] SpokenLanguages { get; set; } = [];

            [Required]
            [Display(Name = "Languages")]
            public Languages[] SelectedLanguages { get; set; } = [];

        }

        public async Task LoadAsync(User user)
        {
            Input = new InputModel { SpokenLanguages = user.Preferences.Language };
            await Task.CompletedTask;
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

            user.Preferences.Language = Input.SelectedLanguages;
            await userManager.UpdateAsync(user);

            StatusMessage = "Your preferences have been updated.";
            return RedirectToPage();
        }
    }
}
