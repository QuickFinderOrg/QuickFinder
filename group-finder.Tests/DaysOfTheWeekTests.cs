using QuickFinder.Domain.Matchmaking;

namespace group_finder.Tests;

public class DaysOfTheWeekTests
{
    [Fact]
    public void CanAddOneDays()
    {
        DaysOfTheWeek days = 0;
        var daysWithMonday = days.WithDay(DaysOfTheWeek.Monday);
        Assert.True(daysWithMonday.HasDay(DaysOfTheWeek.Monday));
    }

    [Fact]
    public void CanAddManyDays()
    {
        DaysOfTheWeek days = 0;
        var daysWithMonday = days.WithDay(DaysOfTheWeek.Saturday | DaysOfTheWeek.Sunday);
        Assert.True(daysWithMonday.HasDay(DaysOfTheWeek.Saturday));
        Assert.True(daysWithMonday.HasDay(DaysOfTheWeek.Sunday));
    }

    [Fact]
    public void CanCountSelectedDays()
    {
        DaysOfTheWeek days = DaysOfTheWeekHelper.Weekend;
        Assert.Equal(2u, days.CountSelectedDays());
    }

    [Fact]
    public void GetNumberOfMatchingDays()
    {
        Assert.Equal(2u, DaysOfTheWeek.All.GetNumberOfMatchingDays(DaysOfTheWeek.Weekends));
    }
}