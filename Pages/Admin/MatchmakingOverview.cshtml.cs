using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Admin;

public class MatchmakingOverviewModel(
    ILogger<MatchmakingOverviewModel> logger,
    MatchmakingService matchmaking,
    GroupMatchmakingService groupMatchmakingService,
    UserService userService,
    TicketRepository ticketRepository,
    GroupRepository groupRepository,
    CourseRepository courseRepository
) : PageModel
{
    private readonly ILogger<MatchmakingOverviewModel> _logger = logger;

    public List<Ticket> Students = [];
    public List<Group> Groups = [];
    public Course[] Courses = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMatchAsync()
    {
        await matchmaking.DoMatching();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMatchGroupAsync()
    {
        await groupMatchmakingService.DoMatching();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(Guid groupId)
    {
        await groupRepository.DeleteGroup(groupId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteGroupsAsync(Guid courseId)
    {
        var groups = await groupRepository.GetGroups(courseId);
        foreach (var group in groups)
        {
            await groupRepository.DeleteGroup(group.Id);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostFillQueueAsync(Guid courseId)
    {
        var users = await userService.GetAllUsers();
        foreach (var user in users)
        {
            await matchmaking.QueueForMatchmakingAsync(user.Id, courseId);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveTicketAsync(Guid ticketId)
    {
        await ticketRepository.DeleteAsync(ticketId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEmptyQueueAsync(Guid courseId)
    {
        var tickets = await ticketRepository.GetAllInCourseAsync(courseId);
        await ticketRepository.RemoveRangeAsync(tickets);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCourseAsync(Guid courseId)
    {
        await courseRepository.DeleteAsync(courseId);
        return RedirectToPage();
    }

    public async Task LoadAsync()
    {
        var waitlist = await ticketRepository.GetAllAsync();
        foreach (Ticket ticket in waitlist)
        {
            Students.Add(ticket);
        }
        var grouplist = await groupRepository.GetGroups();
        Groups = grouplist.ToList();
        Courses = await courseRepository.GetAllAsync();
        _logger.LogInformation("LoadAsync");
    }
}
