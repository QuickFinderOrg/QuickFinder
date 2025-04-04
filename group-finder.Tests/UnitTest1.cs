using QuickFinder.Domain.Matchmaking;

namespace group_finder.Tests;

public class UnitTest1
{
    [Fact]
    public void DistanceBetweenSamePreferencesShouldBeZero()
    {
        var preferences1 = new Preferences
        {
        };
        var preferences2 = new Preferences
        {
        };
        var distance = MatchmakingService.GetScore(preferences1, preferences2);
        Assert.Equal(0, distance);
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
}