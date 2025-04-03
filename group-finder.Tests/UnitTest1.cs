using QuickFinder.Domain.Matchmaking;

namespace group_finder.Tests;

public class UnitTest1
{
    [Fact]
    public void DistanceBetweenSamePreferencesShouldBeZero()
    {
        var candiate = new TestCandidate() { CreatedAt = DateTime.UnixEpoch, Preferences = new Preferences() };
        var candiate2 = new TestCandidate() { CreatedAt = DateTime.UnixEpoch, Preferences = new Preferences() };
        var distance = MatchmakingService.Distance(candiate, candiate2);
        Assert.Equal(0, distance);
    }
}