using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuickFinder.Domain.Matchmaking;

[Owned]
public record class UserPreferences : IUserPreferences
{
    public Languages[] Language { get; set; } = new Languages[0];
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
    public uint GroupSize { get; set; } = 1;
}

public interface IUserPreferences
{
    Languages[] Language { get; set; }
}

public interface ICoursePreferences
{
    Availability Availability { get; set; }
    uint GroupSize { get; set; }
}

public interface IPreferences : IUserPreferences, ICoursePreferences
{
    // This interface combines both IUserPreferences and ICoursePreferences
}

public record class Preferences : IPreferences
{
    public Guid Id { get; init; }
    public Languages[] Language { get; set; } = [];
    public Availability Availability { get; set; } = Availability.Daytime;
    public uint GroupSize { get; set; } = 1;

    public override string ToString()
    {
        return $"Language: {string.Join(", ", Language)}, Availability: {Availability}, GroupSize: {GroupSize}";
    }

    public static Preferences From(UserPreferences userPreferences, CoursePreferences coursePreferences)
    {
        return new Preferences
        {
            Language = userPreferences.Language,
            Availability = coursePreferences.Availability,
            GroupSize = coursePreferences.GroupSize
        };
    }

    public static uint GetNumberOfMatchingLanguages(Languages[] languages1, Languages[] languages2)
    {
        return (uint)languages1.Intersect(languages2).Count();
    }
    public static decimal GetLanguageScore(IPreferences from, IPreferences to)
    {
        var languages = GetNumberOfMatchingLanguages(from.Language, to.Language);
        if (languages >= 1)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    internal static decimal GetAvailabilityScore(IPreferences from, IPreferences to)
    {
        return from.Availability == to.Availability ? 1 : 0;
    }

    internal static decimal GetGroupSizeScore(IPreferences from, IPreferences to)
    {
        return 1;
    }
}

public enum Availability
{
    Daytime,
    Afternoons
}

public enum Languages
{
    English,
    Norwegian,
    Spanish,
    French,
    German,
    Chinese,
    Arabic
}