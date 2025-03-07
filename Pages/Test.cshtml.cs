using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class TestModel(ILogger<TestModel> logger) : PageModel
{
    private readonly ILogger<TestModel> _logger = logger;

    public string TestResult = "";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("POST: test");
        TestResult = "Test success";
        return Page();
    }
}

