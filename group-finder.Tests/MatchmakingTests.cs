using NuGet.ContentModel;
using QuickFinder.Domain.Matchmaking;

namespace group_finder.Tests;

public class MatchmakingTests
{
    private readonly Matchmaker<TestCandidate> deafaultMatchmaker = new Matchmaker<TestCandidate>(
        new MatchmakerConfig()
    );

    [Fact]
    public void ScoreBetweenSameLanguageIsOne()
    {
        var preferences1 = new Preferences { Language = [Languages.English] };
        var preferences2 = new Preferences { Language = [Languages.English] };
        var score = Preferences.GetLanguageScore(preferences1, preferences2);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScoreBetweenSameAvailabilityIsOne()
    {
        var preferences1 = new Preferences { Availability = Availability.Afternoons };
        var preferences2 = new Preferences { Availability = Availability.Afternoons };
        var score = Preferences.GetAvailabilityScore(preferences1, preferences2);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScoreBetweenSamePreferencesShouldBeOne()
    {
        var preferences1 = new Preferences { Language = [Languages.English] };
        var preferences2 = new Preferences { Language = [Languages.English] };
        var score = deafaultMatchmaker.GetScore(preferences1, preferences2);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScoreBetweenOneSharedLanguageShouldBeOne()
    {
        var preferences1 = new Preferences { Language = [Languages.English] };
        var preferences2 = new Preferences { Language = [Languages.English] };
        var distance = Preferences.GetLanguageScore(preferences1, preferences2);
        Assert.Equal(1, distance);
    }

    [Fact]
    public void ScoreBetweenAllSharedDaysOfTheWeekShouldBeOne()
    {
        var preferences1 = new Preferences
        {
            Language = [Languages.English],
            Days = DaysOfTheWeek.All,
        };
        var preferences2 = new Preferences
        {
            Language = [Languages.English],
            Days = DaysOfTheWeek.All,
        };
        var distance = Preferences.GetDaysScore(preferences1, preferences2);
        Assert.Equal(1, distance);
    }

    [Fact]
    public void TwoEqualCandidatesShouldBeTheBestMatch()
    {
        var seedCandidate = new TestCandidate()
        {
            Preferences = new Preferences { Language = [Languages.English] },
            CreatedAt = DateTime.UnixEpoch,
        };
        var bestCandidate = new TestCandidate()
        {
            Preferences = new Preferences { Language = [Languages.English] },
            CreatedAt = DateTime.UnixEpoch,
        };
        var otherCandiadate = new TestCandidate()
        {
            Preferences = new Preferences { Language = [Languages.German] },
            CreatedAt = DateTime.UnixEpoch,
        };
        TestCandidate[] pool = [seedCandidate, bestCandidate, otherCandiadate];

        var matches = deafaultMatchmaker.Match(
            seedCandidate,
            pool,
            groupSize: 2,
            scoreRequirement: 0.0m
        );

        Assert.NotEmpty(matches);
        Assert.Contains(bestCandidate, matches);
    }

    [Fact]
    public void PreferCandidatesWithMoreMatchingDays()
    {
        var seedCandidate = new TestCandidate()
        {
            Preferences = new Preferences { Days = DaysOfTheWeek.Weekdays },
            CreatedAt = DateTime.UnixEpoch,
        };
        var bestCandidate = new TestCandidate()
        {
            Preferences = new Preferences { Days = DaysOfTheWeek.Monday | DaysOfTheWeek.Tuesday },
            CreatedAt = DateTime.UnixEpoch,
        };
        var otherCandiadate = new TestCandidate()
        {
            Preferences = new Preferences { Days = DaysOfTheWeek.Wednesday },
            CreatedAt = DateTime.UnixEpoch,
        };
        TestCandidate[] pool = [seedCandidate, otherCandiadate, bestCandidate];

        var orderedCandidates = deafaultMatchmaker.OrderCandidates(seedCandidate, pool);
        Assert.Equal(bestCandidate, orderedCandidates.First().Value);

        var matches = deafaultMatchmaker.Match(
            seedCandidate,
            pool,
            groupSize: 2,
            scoreRequirement: 0.0m
        );

        Assert.NotEmpty(matches);
        Assert.Contains(bestCandidate, matches);
    }
}
