using QuickFinder.Domain.Matchmaking;

namespace group_finder.Tests;

public class LanguageTests
{
    [Fact]
    public void CountLanguages()
    {
        var a =
            LanguageFlags.English
            | LanguageFlags.Norwegian
            | LanguageFlags.Chinese
            | LanguageFlags.Spanish;
        Assert.Equal(4, a.Count());
    }

    [Fact]
    public void CountSharedLanguages()
    {
        var a = LanguageFlags.English | LanguageFlags.Norwegian;
        var b = LanguageFlags.English | LanguageFlags.Norwegian | LanguageFlags.Arabic;
        Assert.Equal(2, a.IntersectWith(b).Count());
    }

    [Fact]
    public void SucceedsOnOneSharedLanguage()
    {
        var criteria = new MustHaveAtLeastOneLanguageInCommonCritera();
        var a = new UserMatchmakingData
        {
            Languages = [],
            Language = LanguageFlags.English | LanguageFlags.Spanish,
            UserId = "a",
        };
        var b = new UserMatchmakingData
        {
            Languages = [],
            Language = LanguageFlags.English | LanguageFlags.Norwegian,
            UserId = "b",
        };

        Assert.True(criteria.Check(a, b));
    }

    [Fact]
    public void SucceedsOnMoreThanOneSharedLanguage()
    {
        var criteria = new MustHaveAtLeastOneLanguageInCommonCritera();
        var a = new UserMatchmakingData
        {
            Languages = [],
            Language = LanguageFlags.English | LanguageFlags.Norwegian | LanguageFlags.Spanish,
            UserId = "a",
        };
        var b = new UserMatchmakingData
        {
            Languages = [],
            Language = LanguageFlags.English | LanguageFlags.Norwegian,
            UserId = "b",
        };

        Assert.True(criteria.Check(a, b));
    }

    [Fact]
    public void FailsOnZeroSharedLanguage()
    {
        var criteria = new MustHaveAtLeastOneLanguageInCommonCritera();
        var a = new UserMatchmakingData
        {
            Languages = [],
            Language = LanguageFlags.English,
            UserId = "a",
        };
        var b = new UserMatchmakingData
        {
            Languages = [],
            Language = LanguageFlags.Norwegian,
            UserId = "b",
        };

        Assert.False(criteria.Check(a, b));
    }
}
