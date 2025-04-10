using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class Course() : BaseEntity
{
    public Guid Id { get; init; }
    public required string Name { get; set; } = "Course";
    public List<Group> Groups { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];
    public uint GroupSize { get; set; } = 2;
    public bool AllowCustomSize { get; set; } = false;
    public List<User> Members { get; set; } = [];

    public IEnumerable<CoursePreferences> CoursePreferences { get; set; } = null!;
}

public class CourseRepository : Repository<Course, Guid>
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<TicketRepository> logger;

    public CourseRepository(ApplicationDbContext applicationDbContext, ILogger<TicketRepository> ticketLogger) : base(applicationDbContext)
    {
        db = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        logger = ticketLogger ?? throw new ArgumentNullException(nameof(ticketLogger));
    }


    public async Task<Course> CreateCourse(string name, uint groupSize, bool allowCustomSize)
    {
        var course = new Course() { Name = name, GroupSize = groupSize, AllowCustomSize = allowCustomSize };
        db.Add(course);
        await db.SaveChangesAsync();
        return course;
    }

    public new async Task<Course[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Courses.Include(c => c.Members).ToArrayAsync(cancellationToken);
    }
}
