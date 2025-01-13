using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class MatchingModel : PageModel
{
    private readonly ILogger<MatchingModel> _logger;

    public MatchingModel(ILogger<MatchingModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }
}
