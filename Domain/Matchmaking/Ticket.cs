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

public class GroupTicket() : BaseEntity, ICandidate
{
    public Guid Id { get; init; }
    public required Group Group { get; init; }
    public required Course Course { get; init; }
    public required Preferences Preferences { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
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
            throw new AlreadyInQueueException();
        }

        db.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);
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

    public async Task<Ticket?> GetByCourseAsync(
        string userId,
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        return await db
            .Tickets.Where(t => t.User.Id == userId && t.Course.Id == courseId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public async Task<Ticket[]> GetAllInCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        return await db
                .Tickets.Include(t => t.User)
                .Include(t => t.Course)
                .Include(t => t.Preferences)
                .Where(t => t.Course.Id == courseId)
                .ToArrayAsync(cancellationToken) ?? throw new Exception("WAITLIST");
    }

    public new async Task<Ticket[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db
                .Tickets.Include(t => t.User)
                .Include(t => t.Course)
                .Include(t => t.Preferences)
                .ToArrayAsync(cancellationToken) ?? throw new Exception("WAITLIST");
    }

    public async Task<Ticket[]> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        return await db
                .Tickets.Where(t => t.User.Id == userId)
                .Include(t => t.User)
                .Include(t => t.Course)
                .Include(t => t.Preferences)
                .ToArrayAsync(cancellationToken) ?? throw new Exception("WAITLIST");
    }

    public async Task RemoveRangeAsync(
        IEnumerable<Ticket> tickets,
        CancellationToken cancellationToken = default
    )
    {
        db.Tickets.RemoveRange(tickets);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CheckIfInQueue(User user, Course course)
    {
        var ticket = await db
            .Tickets.Where(g => g.Course == course && g.User == user)
            .FirstOrDefaultAsync();
        if (ticket is null)
        {
            return false;
        }
        return true;
    }
}

public class GroupTicketRepository : Repository<GroupTicket, Guid>
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<GroupTicketRepository> logger;

    public GroupTicketRepository(
        ApplicationDbContext applicationDbContext,
        ILogger<GroupTicketRepository> ticketLogger
    )
        : base(applicationDbContext)
    {
        db = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        logger = ticketLogger ?? throw new ArgumentNullException(nameof(ticketLogger));
    }

    public new async Task AddAsync(
        GroupTicket ticket,
        CancellationToken cancellationToken = default
    )
    {
        var course = ticket.Course ?? throw new Exception("Course");
        var group = ticket.Group ?? throw new Exception("User");

        var existing_tickets = await db
            .GroupTickets.Include(c => c.Group)
            .Include(c => c.Course)
            .Where(t => t.Group == group && t.Course == course)
            .ToArrayAsync(cancellationToken);

        if (existing_tickets.Length != 0)
        {
            logger.LogError(
                "Group '{groupId}' is already queued up for course '{courseId}'",
                group.Id,
                course.Id
            );
            throw new AlreadyInQueueException();
        }

        db.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);
    }

    public new async Task<GroupTicket?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await db
            .GroupTickets.Include(t => t.Group)
            .Include(t => t.Group.Members)
            .Include(t => t.Course)
            .Include(t => t.Preferences)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: cancellationToken);
    }

    public async Task<GroupTicket?> GetByCourseAsync(
        Guid groupId,
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        return await db
            .GroupTickets.Where(t => t.Group.Id == groupId && t.Course.Id == courseId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public new async Task<GroupTicket[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db
                .GroupTickets.Include(t => t.Group)
                .Include(t => t.Group.Members)
                .Include(t => t.Course)
                .Include(t => t.Preferences)
                .ToArrayAsync(cancellationToken) ?? throw new Exception("WAITLIST");
    }

    public async Task RemoveRangeAsync(
        IEnumerable<GroupTicket> tickets,
        CancellationToken cancellationToken = default
    )
    {
        db.GroupTickets.RemoveRange(tickets);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CheckIfInQueue(Guid groupId, Guid courseId)
    {
        var ticket = await db
            .GroupTickets.Where(g => g.Course.Id == courseId && g.Group.Id == groupId)
            .FirstOrDefaultAsync();
        if (ticket is null)
        {
            return false;
        }
        return true;
    }
}

public class AlreadyInQueueException : Exception { }
