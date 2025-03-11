using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages
{
    public class LoginModel(IConfiguration configuration) : PageModel
    {

        public IActionResult OnGet()
        {
        var user_session = HttpContext.GetUserId();
        if (user_session is null)
        {
            return Page();
        }
        return Redirect(StudentRoutes.Home());
        }

        public IActionResult OnPost()
        {
            return Page();
        }
    }
}