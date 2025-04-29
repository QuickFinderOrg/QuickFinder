using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

[Owned]
public record class UserPreferences : IUserPreferences
{
    public LanguageFlags Language { get; set; } = LanguageFlagsExtensions.None;
    public Availability GlobalAvailability { get; set; } = Availability.Daytime;
    public DaysOfTheWeek GlobalDays { get; set; } = DaysOfTheWeek.All;
}

[PrimaryKey(nameof(UserId), nameof(CourseId))]
public record class CoursePreferences : ICoursePreferences
{
    [ForeignKey(nameof(User))]
    public string UserId { get; init; } = null!;

    [ForeignKey(nameof(Course))]
    public Guid CourseId { get; init; }

    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;

    public Availability Availability { get; set; } = Availability.Daytime;
    public DaysOfTheWeek Days { get; set; } = DaysOfTheWeek.All;
    public uint GroupSize { get; set; } = 2;
}

public interface IUserPreferences
{
    LanguageFlags Language { get; set; }
    Availability GlobalAvailability { get; set; }
    DaysOfTheWeek GlobalDays { get; set; }
}

public interface ICoursePreferences
{
    Availability Availability { get; set; }
    DaysOfTheWeek Days { get; set; }
    uint GroupSize { get; set; }
}

public interface IPreferences : IUserPreferences, ICoursePreferences
{
    // This interface combines both IUserPreferences and ICoursePreferences
}

public record class Preferences : IPreferences
{
    public Guid Id { get; init; }
    public LanguageFlags Language { get; set; } = LanguageFlags.English;
    public Availability Availability { get; set; } = Availability.Daytime;
    public Availability GlobalAvailability { get; set; } = Availability.Daytime;
    public DaysOfTheWeek Days { get; set; } = DaysOfTheWeek.All;
    public DaysOfTheWeek GlobalDays { get; set; } = DaysOfTheWeek.All;
    public uint GroupSize { get; set; } = 2;

    public override string ToString()
    {
        return $"Language: {string.Join(", ", Language)}, Availability: {Availability}, GroupSize: {GroupSize}, Days: {Days}";
    }

    public static Preferences From(
        UserPreferences userPreferences,
        CoursePreferences coursePreferences
    )
    {
        return new Preferences
        {
            Language = userPreferences.Language,
            Availability = coursePreferences.Availability,
            GroupSize = coursePreferences.GroupSize,
            Days = coursePreferences.Days,
        };
    }

    public static decimal GetAvailabilityScore(IPreferences from, IPreferences to)
    {
        return from.Availability == to.Availability ? 1 : 0;
    }

    public static decimal GetDaysScore(IPreferences from, IPreferences to)
    {
        var availabile_days = from.Days.CountSelectedDays();
        if (availabile_days == 0)
        {
            return 0;
        }
        return (decimal)from.Days.GetNumberOfMatchingDays(to.Days) / (decimal)availabile_days;
    }

    public static decimal GetGroupSizeScore(IPreferences from, IPreferences to)
    {
        return 1;
    }
}

public enum Availability
{
    Daytime,
    Afternoons,
}

[Flags]
public enum LanguageFlags
{
    English = 1 << 0,
    Norwegian = 1 << 1,
    Spanish = 1 << 2,
    French = 1 << 3,
    German = 1 << 4,
    Chinese = 1 << 5,
    Arabic = 1 << 6,
}

[Flags]
public enum DaysOfTheWeek
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekends = Saturday | Sunday,
    All = Weekdays | Weekends,
}

public static class DaysOfTheWeekHelper
{
    public const DaysOfTheWeek Weekend = DaysOfTheWeek.Saturday | DaysOfTheWeek.Sunday;

    public static bool HasDay(this DaysOfTheWeek daysA, DaysOfTheWeek daysB)
    {
        return (daysA & daysB) == daysB;
    }

    public static DaysOfTheWeek WithDay(this DaysOfTheWeek daysA, DaysOfTheWeek daysB)
    {
        return daysA | daysB;
    }

    public static DaysOfTheWeek RemoveDay(this DaysOfTheWeek daysA, DaysOfTheWeek daysB)
    {
        return daysA & ~daysB;
    }

    public static DaysOfTheWeek SetFromArray(this DaysOfTheWeek daysA, bool[] daysArray)
    {
        DaysOfTheWeek newDays = DaysOfTheWeek.None;
        for (int i = 0; i < 7; i++)
        {
            DaysOfTheWeek day = (DaysOfTheWeek)(1 << i);
            if (daysArray[i])
            {
                newDays = newDays.WithDay(day);
            }
        }
        return newDays;
    }

    public static uint GetNumberOfMatchingDays(this DaysOfTheWeek daysA, DaysOfTheWeek daysB)
    {
        return (daysA & daysB).CountSelectedDays();
    }

    public static uint CountSelectedDays(this DaysOfTheWeek days)
    {
        var matches = 0u;
        for (int i = 0; i < 7; i++)
        {
            DaysOfTheWeek day = (DaysOfTheWeek)(1 << i);
            if (days.HasDay(day))
            {
                matches++;
            }
        }
        return matches;
    }
}

public static class LanguageFlagsExtensions
{
    public static readonly LanguageFlags None = 0;

    public static LanguageFlags IntersectWith(this LanguageFlags LanguageFlags, LanguageFlags value)
    {
        return LanguageFlags & value;
    }

    public static bool Any(this LanguageFlags LanguageFlags, LanguageFlags value)
    {
        return (LanguageFlags & value) != 0;
    }

    public static int Count(this LanguageFlags lValue)
    {
        int iCount = 0;

        //Loop the value while there are still bits
        while (lValue != 0)
        {
            //Remove the end bit
            lValue = lValue & (lValue - 1);

            //Increment the count
            iCount++;
        }

        //Return the count
        return iCount;
    }
}

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
