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

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public class TicketRepository : Repository<Ticket>
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<TicketRepository> logger;

    public TicketRepository(ApplicationDbContext applicationDbContext, ILogger<TicketRepository> ticketLogger) : base(applicationDbContext)
    {
        db = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        logger = ticketLogger ?? throw new ArgumentNullException(nameof(ticketLogger));
    }

    public async Task<bool> AddToWaitlist(User user, Course course)
    {
        var existing = await db.Tickets.Include(c => c.User).Include(c => c.Course).Where(t => t.User == user && t.Course == course).ToArrayAsync();

        if (existing.Length != 0)
        {
            logger.LogError("User '{userId}' is already queued up for course '{courseId}'", user.Id, course.Id);
            return false;
        }

        var course_prefs = await db.CoursePreferences.FirstOrDefaultAsync(p => p.User == user && p.Course == course);

        if (course_prefs == null)
        {
            course_prefs = new CoursePreferences() { User = user, Course = course };
            db.Add(course_prefs);
        }

        var full_preferences = Preferences.From(user.Preferences, course_prefs);

        db.Add(new Ticket() { User = user, Course = course, Preferences = full_preferences });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Ticket[]> GetWaitlist()
    {
        return await db.Tickets.Include(t => t.User).Include(t => t.Course).Include(t => t.Preferences).ToArrayAsync() ?? throw new Exception("WAITLIST");
    }

    /// <summary>
    /// Get tickets for a particular course
    /// </summary>
    /// <param name="course"></param>
    /// <returns></returns>
    public async Task<Ticket[]> GetWaitlist(Course course)
    {
        return await db.Tickets.Include(t => t.User).Include(t => t.Course).Where(t => t.Course == course).ToArrayAsync();
    }
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext db)
    {
        _dbContext = db ?? throw new ArgumentNullException(nameof(db));
        _dbSet = _dbContext.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}