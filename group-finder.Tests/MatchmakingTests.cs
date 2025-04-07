using NuGet.ContentModel;
using QuickFinder.Domain.Matchmaking;

namespace group_finder.Tests;

public class MatchmakingTests
{
    [Fact]
    public void ScoreBetweenSameLanguageIsOne()
    {
        var preferences1 = new Preferences
        {
            Language = [Languages.English]
        };
        var preferences2 = new Preferences
        {
            Language = [Languages.English]
        };
        var score = Preferences.GetLanguageScore(preferences1, preferences2);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScoreBetweenSameAvailabilityIsOne()
    {
        var preferences1 = new Preferences
        {
            Availability = Availability.Afternoons
        };
        var preferences2 = new Preferences
        {
            Availability = Availability.Afternoons
        };
        var score = Preferences.GetAvailabilityScore(preferences1, preferences2);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScoreBetweenSamePreferencesShouldBeOne()
    {
        var preferences1 = new Preferences
        {
            Language = [Languages.English]
        };
        var preferences2 = new Preferences
        {
            Language = [Languages.English]
        };
        var score = MatchmakingService.GetScore(preferences1, preferences2);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScoreBetweenOneSharedLanguageShouldBeOne()
    {
        var preferences1 = new Preferences
        {
            Language = [Languages.English],
        };
        var preferences2 = new Preferences
        {
            Language = [Languages.English],
        };
        var distance = Preferences.GetLanguageScore(preferences1, preferences2);
        Assert.Equal(1, distance);
    }

    [Fact]
    public void ScoreBetweenAllSharedDaysOfTheWeekShouldBeOne()
    {
        var preferences1 = new Preferences
        {
            Language = [Languages.English],
            Days = DaysOfTheWeek.All
        };
        var preferences2 = new Preferences
        {
            Language = [Languages.English],
            Days = DaysOfTheWeek.All
        };
        var distance = Preferences.GetDaysScore(preferences1, preferences2);
        Assert.Equal(1, distance);
    }

    [Fact]
    public void TwoEqualCandidatesShouldBeTheBestMatch()
    {
        var seedCandidate = new TestCandidate() { Preferences = new Preferences { Language = [Languages.English] }, CreatedAt = DateTime.UnixEpoch };
        var bestCandidate = new TestCandidate() { Preferences = new Preferences { Language = [Languages.English] }, CreatedAt = DateTime.UnixEpoch };
        var otherCandiadate = new TestCandidate() { Preferences = new Preferences { Language = [Languages.German] }, CreatedAt = DateTime.UnixEpoch };
        ICandidate[] pool = [seedCandidate, bestCandidate, otherCandiadate];

        var matches = MatchmakingService.Match(seedCandidate, pool, 2, DateTime.UnixEpoch);

        Assert.NotEmpty(matches);
        Assert.Contains(bestCandidate, matches);
    }

    [Fact(Skip = "Days need more work")]
    public void PreferCandidatesWithMoreMatchingDays()
    {
        var seedCandidate = new TestCandidate() { Preferences = new Preferences { Days = DaysOfTheWeek.Weekdays }, CreatedAt = DateTime.UnixEpoch };
        var bestCandidate = new TestCandidate() { Preferences = new Preferences { Days = DaysOfTheWeek.Monday | DaysOfTheWeek.Tuesday }, CreatedAt = DateTime.UnixEpoch };
        var otherCandiadate = new TestCandidate() { Preferences = new Preferences { Days = DaysOfTheWeek.Wednesday }, CreatedAt = DateTime.UnixEpoch };
        ICandidate[] pool = [seedCandidate, otherCandiadate, bestCandidate];

        var orderedCandidates = MatchmakingService.OrderCandidates(seedCandidate, pool);
        Assert.Equal(bestCandidate, orderedCandidates[0].Value);


        var matches = MatchmakingService.Match(seedCandidate, pool, 2, DateTime.UnixEpoch);

        Assert.NotEmpty(matches);
        Assert.Contains(bestCandidate, matches);
    }
}