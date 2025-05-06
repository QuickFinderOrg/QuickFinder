using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

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

    public async Task<GroupTicket[]> GetAllInCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        return await db
            .GroupTickets.Include(t => t.Group)
            .Include(t => t.Group.Members)
            .Include(t => t.Course)
            .Include(t => t.Preferences)
            .Where(t => t.Course.Id == courseId)
            .ToArrayAsync(cancellationToken);
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
