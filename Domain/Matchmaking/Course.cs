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

public class CourseRepository : Repository<Ticket>
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

    public async Task<Course[]> GetCourses()
    {
        return await db.Courses.Include(c => c.Members).ToArrayAsync();
    }

    public async Task<Course[]> GetCourses(User user)
    {
        var groups = await db.Groups
                            .Include(g => g.Members)
                            .Include(g => g.Course)
                            .Where(g => g.Members
                            .Contains(user)).Include(g => g.Course).ToArrayAsync();

        var courses = await db.Courses.Include(c => c.Members).ToListAsync();

        // filter away courses?
        foreach (Group group in groups)
        {
            courses.Remove(group.Course);
        }

        return [.. courses];
    }

    public async Task<Course> GetCourse(Guid courseId)
    {
        return await db.Courses.FirstAsync(c => c.Id == courseId) ?? throw new Exception("Course not found");
    }
}
