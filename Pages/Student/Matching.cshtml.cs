using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student;

public class MatchingModel(ILogger<MatchingModel> logger) : PageModel
{
    private readonly ILogger<MatchingModel> _logger = logger;

    public void OnGet()
    {

    }
}
