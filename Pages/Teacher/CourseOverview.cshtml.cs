using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Pages.Teacher;

public class CourseOverviewModel(
    ILogger<CourseOverviewModel> logger,
    DiscordService discordService,
    CourseRepository courseRepository,
    GroupRepository groupRepository,
    TicketRepository ticketRepository
) : PageModel
{
    private readonly ILogger<CourseOverviewModel> _logger = logger;
    public List<Group> Groups = [];
    public List<Ticket> Tickets = [];
    public Course[] Courses = [];

    [BindProperty]
    public Course Course { get; set; } = default!;

    public DiscordServerItem? CourseDiscordServer { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostChangeCourseAsync()
    {
        Course =
            await courseRepository.GetByIdAsync(Course.Id)
            ?? throw new Exception("Course not found");
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(Guid groupId)
    {
        await groupRepository.DeleteGroup(groupId);
        Course =
            await courseRepository.GetByIdAsync(Course.Id)
            ?? throw new Exception("Course not found");
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task<IActionResult> OnPostChangeGroupSizeAsync()
    {
        await groupRepository.ChangeGroupSize(Course.Id, Course.GroupSize);
        return RedirectToPage(TeacherRoutes.CourseOverview(), new { id = Course.Id });
    }

    public async Task LoadAsync(Guid courseId)
    {
        Courses = await courseRepository.GetAllAsync();
        if (courseId == Guid.Empty)
        {
            Course = Courses[0];
        }
        else
        {
            Course =
                await courseRepository.GetByIdAsync(courseId)
                ?? throw new Exception("Course not found");
        }
        var waitlist = await ticketRepository.GetAllAsync();
        foreach (Ticket ticket in waitlist)
        {
            if (ticket.Course.Id == Course.Id)
            {
                Tickets.Add(ticket);
            }
        }
        var grouplist = await groupRepository.GetGroups(Course.Id);
        var CourseDiscordServers = await discordService.GetCourseServer(Course.Id);
        CourseDiscordServer = CourseDiscordServers.FirstOrDefault();

        Groups = grouplist.ToList();
        _logger.LogInformation("LoadGroups");
    }
}
