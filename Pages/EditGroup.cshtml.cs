using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class EditGroupModel() : PageModel
{

    public async Task<IActionResult> OnGetAsync()
    {
        return Page();
    }
}
