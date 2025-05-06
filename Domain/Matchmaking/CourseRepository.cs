using MediatR;
using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class CourseRepository : Repository<Course, Guid>
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<TicketRepository> logger;
    private readonly GroupRepository groupRepository; // TODO: maybe we should merge course and group repository instead?
    private readonly IMediator mediator;

    public CourseRepository(
        ApplicationDbContext applicationDbContext,
        ILogger<TicketRepository> ticketLogger,
        GroupRepository ticketGroupRepository,
        IMediator ticketMediator
    )
        : base(applicationDbContext)
    {
        db = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        logger = ticketLogger ?? throw new ArgumentNullException(nameof(ticketLogger));
        groupRepository =
            ticketGroupRepository ?? throw new ArgumentNullException(nameof(ticketGroupRepository));
        mediator = ticketMediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public new async Task AddAsync(Course course, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(course, cancellationToken);
        if (errors.Length > 0)
        {
            throw new Exception("Cannot create course: " + string.Join(", ", errors));
        }

        await base.AddAsync(course, cancellationToken);
    }

    public new async Task<Course[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Courses.Include(c => c.Members).ToArrayAsync(cancellationToken);
    }

    public async Task<Course?> GetByIdAsync(Guid id)
    {
        return await db.Courses.Where(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task JoinCourse(User user, Course course)
    {
        course.Members.Add(user);
        await db.SaveChangesAsync();
        await mediator.Publish(new CourseJoined() { User = user, Course = course });
    }

    public async Task LeaveCourse(User user, Course course)
    {
        if (await groupRepository.CheckIfInGroup(user, course))
        {
            var group =
                await db
                    .Groups.Include(g => g.Members)
                    .Where(g => g.Course == course && g.Members.Contains(user))
                    .FirstOrDefaultAsync() ?? throw new Exception("Group not found");
            group.Members.Remove(user);
            await groupRepository.UpdateAsync(group);
        }
        course.Members.Remove(user);
        await db.SaveChangesAsync();
    }

    private async Task<string[]> ValidateAsync(
        Course courseToValidate,
        CancellationToken cancellationToken = default
    )
    {
        var errors = new List<string>();

        var otherCourse = await db
            .Courses.Where(c => c.Name == courseToValidate.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (otherCourse != null)
        {
            errors.Add("Course name not unique!");
        }

        if (courseToValidate.GroupSize < 2 || courseToValidate.GroupSize > 20)
        {
            errors.Add("Group size out of range!");
        }

        return errors.ToArray();
    }
}
