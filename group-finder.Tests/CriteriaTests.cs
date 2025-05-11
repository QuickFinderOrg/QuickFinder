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
    public void PreferMoreSharedLanguages()
    {
        var preference = new LanguagesInCommonPreference();
        var speakerEngNor = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Norwegian,
            UserId = "a",
        };
        var speakerSpanish = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.Spanish,
            UserId = "s",
        };
        var speakerEngSpa = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Spanish,
            UserId = "e",
        };
        var SpeakerEngNor2 = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Norwegian,
            UserId = "c",
        };

        Assert.Equal(0, preference.Check(speakerEngNor, speakerSpanish));
        Assert.Equal(0.5m, preference.Check(speakerEngNor, speakerEngSpa));
        Assert.Equal(1, preference.Check(speakerEngNor, SpeakerEngNor2));
    }

    [Fact]
    public void MatchmakerPreferMoreSharedLanguages()
    {
        var matchmaker = new Matchmaker<DefaultUserMatchmakingData, DefaultGroupMatchmakingData>(
            new MatchmakerConfig
            {
                CriteriaList = [],
                WeightedPreferenceList = [(1m, new LanguagesInCommonPreference())],
            }
        );
        var speakerEngNor = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Norwegian,
            UserId = "a",
        };
        var speakerSpanish = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.Spanish,
            UserId = "s",
        };
        var speakerEngSpa = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Spanish,
            UserId = "e",
        };
        var SpeakerEngNor2 = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Norwegian,
            UserId = "c",
        };

        var match = matchmaker
            .Match(speakerEngNor, [speakerEngSpa, speakerSpanish, SpeakerEngNor2], 1)
            .First();
        Assert.Equal(SpeakerEngNor2, match);
    }

    [Fact]
    public void SucceedsOnOneSharedLanguage()
    {
        var criteria = new MustHaveAtLeastOneLanguageInCommonCriteria();
        var a = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Spanish,
            UserId = "a",
        };
        var b = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Norwegian,
            UserId = "b",
        };

        Assert.True(criteria.Check(a, b));
    }

    [Fact]
    public void SucceedsOnMoreThanOneSharedLanguage()
    {
        var criteria = new MustHaveAtLeastOneLanguageInCommonCriteria();
        var a = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Norwegian | LanguageFlags.Spanish,
            UserId = "a",
        };
        var b = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.English | LanguageFlags.Norwegian,
            UserId = "b",
        };

        Assert.True(criteria.Check(a, b));
    }

    [Fact]
    public void FailsOnZeroSharedLanguage()
    {
        var criteria = new MustHaveAtLeastOneLanguageInCommonCriteria();
        var a = new DefaultUserMatchmakingData { Languages = LanguageFlags.English, UserId = "a" };
        var b = new DefaultUserMatchmakingData
        {
            Languages = LanguageFlags.Norwegian,
            UserId = "b",
        };

        Assert.False(criteria.Check(a, b));
    }
}
