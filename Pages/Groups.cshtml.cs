using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace group_finder.Pages;

public class GroupsModel : PageModel
{
    private readonly ILogger<GroupsModel> _logger;

    public GroupsModel(ILogger<GroupsModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }
}
