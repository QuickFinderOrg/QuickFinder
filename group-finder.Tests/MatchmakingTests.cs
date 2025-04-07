using NuGet.ContentModel;
using QuickFinder.Domain.Matchmaking;

namespace group_finder.Tests;

public class MatchmakingTests
{
    [Fact]
    public void DistanceBetweenSameLanguageIsOne()
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
    public void DistanceBetweenSameAvailabilityIsOne()
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
    public void DistanceBetweenSamePreferencesShouldBeOne()
    {
        var preferences1 = new Preferences
        {
            Language = [Languages.English]
        };
        var preferences2 = new Preferences
        {
            Language = [Languages.English]
        };
        var distance = MatchmakingService.GetScore(preferences1, preferences2);
        Assert.Equal(1, distance);
    }

    [Fact]
    public void DistanceBetweenOneSharedLanguageShouldBeOne()
    {
        var preferences1 = new Preferences
        {
            Language = [Languages.English],
        };
        var preferences2 = new Preferences
        {
            Language = [Languages.English],
        };
        var distance = MatchmakingService.GetScore(preferences1, preferences2);
        Assert.Equal(1, distance);
    }

    [Fact]
    public void TwoEqualCandidatesShouldBeTheBestMatch()
    {
        var seedCandidate = new TestCandidate() { Preferences = new Preferences { Language = [Languages.English] }, CreatedAt = DateTime.UnixEpoch };
        var bestCandidate = new TestCandidate() { Preferences = new Preferences { Language = [Languages.English] }, CreatedAt = DateTime.UnixEpoch };
        var otherCandiadate = new TestCandidate() { Preferences = new Preferences { Language = [Languages.German] }, CreatedAt = DateTime.UnixEpoch };
        ICandidate[] pool = [seedCandidate, bestCandidate, otherCandiadate];

        var languageScore = Preferences.GetLanguageScore(seedCandidate.Preferences, bestCandidate.Preferences);
        Assert.Equal(1, languageScore);
        var availabilityScore = Preferences.GetAvailabilityScore(seedCandidate.Preferences, bestCandidate.Preferences);
        Assert.Equal(1, availabilityScore);
        var groupSizeScore = Preferences.GetGroupSizeScore(seedCandidate.Preferences, bestCandidate.Preferences);
        Assert.Equal(1, groupSizeScore);

        var score = MatchmakingService.GetScore(seedCandidate.Preferences, bestCandidate.Preferences);
        Assert.Equal(1, score);

        var matches = MatchmakingService.Match(seedCandidate, pool, 2, DateTime.UnixEpoch);

        Assert.NotEmpty(matches);
        Assert.Contains(bestCandidate, matches);
    }
}