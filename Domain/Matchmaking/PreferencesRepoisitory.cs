using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class PreferencesRepository : Repository<Preferences, Guid>
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<TicketRepository> logger;
    private readonly GroupRepository groupRepository; // TODO: maybe we should merge course and group repository instead?
    private readonly UserService userService;

    public PreferencesRepository(
        ApplicationDbContext applicationDbContext,
        ILogger<TicketRepository> preferencesLogger,
        GroupRepository preferencesGroupRepository,
        UserService preferencesUserService
    )
        : base(applicationDbContext)
    {
        db = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        logger = preferencesLogger ?? throw new ArgumentNullException(nameof(preferencesLogger));
        groupRepository =
            preferencesGroupRepository
            ?? throw new ArgumentNullException(nameof(preferencesGroupRepository));
        userService =
            preferencesUserService
            ?? throw new ArgumentNullException(nameof(preferencesUserService));
    }

    public async Task<Preferences?> GetPreferencesAsync(Guid courseId, string userId)
    {
        var user = await userService.GetUser(userId);
        var user_prefs = user.Preferences;

        var course_prefs =
            await db.CoursePreferences.FirstOrDefaultAsync(p =>
                p.UserId == userId && p.CourseId == courseId
            )
            ?? new CoursePreferences()
            {
                Availability = user_prefs.GlobalAvailability,
                Days = user_prefs.GlobalDays,
            };

        // parent function or matchmaking service should get this from preference repository.
        return Preferences.From(user.Preferences, course_prefs);
    }

    public async Task<CoursePreferences?> GetCoursePreferences(Guid courseId, string userId)
    {
        return await db
            .CoursePreferences.Include(prefs => prefs.User)
            .Include(prefs => prefs.Course)
            .Where(prefs => prefs.UserId == userId && prefs.CourseId == courseId)
            .SingleOrDefaultAsync();
    }

    public async Task<CoursePreferences?> CreateNewCoursePreferences(Guid courseId, string userId)
    {
        var coursePreferences = new CoursePreferences() { CourseId = courseId, UserId = userId };
        db.Add(coursePreferences);
        await db.SaveChangesAsync();
        return coursePreferences;
    }

    public async Task UpdateCoursePreferencesAsync(
        Guid courseId,
        string userId,
        CoursePreferences newPreferences
    )
    {
        await db.SaveChangesAsync();
    }
}
