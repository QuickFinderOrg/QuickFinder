using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages.Student;

public class GroupsModel(ILogger<GroupsModel> logger) : PageModel
{
    private readonly ILogger<GroupsModel> _logger = logger;

    public void OnGet()
    {

    }
}
