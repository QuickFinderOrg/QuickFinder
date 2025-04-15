using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class Ticket() : BaseEntity, ICandidate
{
    public Guid Id { get; init; }
    public required User User { get; init; }
    public required Course Course { get; init; }
    public required Preferences Preferences { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public bool WillAcceptGroup(Group group)
    {
        if (group.IsFull)
        {
            return false;
        }
        if (group.Preferences.Availability != Preferences.Availability)
        {
            return false;
        }

        return true;
    }
}

public class TicketRepository : Repository<Ticket, Guid>
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<TicketRepository> logger;

    public TicketRepository(
        ApplicationDbContext applicationDbContext,
        ILogger<TicketRepository> ticketLogger
    )
        : base(applicationDbContext)
    {
        db = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        logger = ticketLogger ?? throw new ArgumentNullException(nameof(ticketLogger));
    }

    public new async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        var course = ticket.Course ?? throw new Exception("Course");
        var user = ticket.User ?? throw new Exception("User");

        var existing_tickets = await db
            .Tickets.Include(c => c.User)
            .Include(c => c.Course)
            .Where(t => t.User == user && t.Course == course)
            .ToArrayAsync(cancellationToken);

        if (existing_tickets.Length != 0)
        {
            logger.LogError(
                "User '{userId}' is already queued up for course '{courseId}'",
                user.Id,
                course.Id
            );
            throw new Exception("Already in queue.");
        }

        var course_prefs = await db.CoursePreferences.FirstOrDefaultAsync(p =>
            p.User == user && p.Course == course
        );

        if (course_prefs == null)
        {
            course_prefs = new CoursePreferences() { User = user, Course = course };
            db.Add(course_prefs);
        }

        // parent function or matchmaking service should get this from preference repository.
        var full_preferences = Preferences.From(user.Preferences, course_prefs);

        db.Add(
            new Ticket()
            {
                User = user,
                Course = course,
                Preferences = full_preferences,
            }
        );
        await db.SaveChangesAsync();
    }

    public new async Task<Ticket?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await db
            .Tickets.Include(t => t.User)
            .Include(t => t.Course)
            .Include(t => t.Preferences)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: cancellationToken);
    }

    public new async Task<Ticket[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db
                .Tickets.Include(t => t.User)
                .Include(t => t.Course)
                .Include(t => t.Preferences)
                .ToArrayAsync(cancellationToken) ?? throw new Exception("WAITLIST");
    }

    /// <summary>
    /// Get tickets for a particular course
    /// </summary>
    /// <param name="course"></param>
    /// <returns></returns>
    public async Task<Ticket[]> GetWaitlist(Course course)
    {
        return await db
            .Tickets.Include(t => t.User)
            .Include(t => t.Course)
            .Where(t => t.Course == course)
            .ToArrayAsync();
    }

    public async Task<bool> RemoveUserFromWaitlist(string userId)
    {
        var user = await db.Users.FindAsync(userId) ?? throw new Exception("User not found");
        var tickets = await db
            .Tickets.Include(g => g.User)
            .Where(t => t.User == user)
            .ToArrayAsync();
        db.Tickets.RemoveRange(tickets);

        await db.SaveChangesAsync();

        return true;
    }
}

public class AlreadyInQueueException : Exception { }
