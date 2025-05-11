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

    public StudyLocation StudyLocation { get; set; } =
        StudyLocation.InPerson | StudyLocation.Online;
}

public interface IUserPreferences
{
    LanguageFlags Language { get; set; }
}

public interface ICoursePreferences
{
    Availability Availability { get; set; }
    DaysOfTheWeek Days { get; set; }
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
    public DaysOfTheWeek Days { get; set; } = DaysOfTheWeek.All;
    public StudyLocation StudyLocation { get; set; } =
        StudyLocation.InPerson | StudyLocation.Online;

    public override string ToString()
    {
        return $"Language: {string.Join(", ", Language)}, Availability: {Availability}, Days: {Days}, StudyLocation: {StudyLocation}";
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
            Days = coursePreferences.Days,
            StudyLocation = coursePreferences.StudyLocation,
        };
    }
}

public enum Availability
{
    Daytime,
    Afternoons,
}

[Flags]
public enum StudyLocation
{
    InPerson = 1 << 0,
    Online = 1 << 1,
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
    public static readonly LanguageFlags All =
        LanguageFlags.English
        | LanguageFlags.Norwegian
        | LanguageFlags.Spanish
        | LanguageFlags.French
        | LanguageFlags.German
        | LanguageFlags.Chinese
        | LanguageFlags.Arabic;

    public static LanguageFlags IntersectWith(this LanguageFlags languageFlags, LanguageFlags value)
    {
        return languageFlags & value;
    }

    public static bool Any(this LanguageFlags languageFlags, LanguageFlags value)
    {
        return (languageFlags & value) != 0;
    }

    public static int Count(this LanguageFlags lValue)
    {
        int iCount = 0;

        // Loop the value while there are still bits
        while (lValue != 0)
        {
            // Remove the end bit
            lValue = lValue & (lValue - 1);

            // Increment the count
            iCount++;
        }

        // Return the count
        return iCount;
    }
}

public static class StudyLocationExtensions
{
    public static StudyLocation IntersectWith(this StudyLocation location, StudyLocation value)
    {
        return location & value;
    }
}
