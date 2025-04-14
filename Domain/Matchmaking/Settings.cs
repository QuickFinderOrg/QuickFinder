using System.ComponentModel.DataAnnotations;

namespace QuickFinder.Domain.Matchmaking;

public sealed class MatchmakingOptions
{
    public const string Matchmaking = "Matchmaking";

    [Required]
    public string? Interval { get; set; }
    public TimeSpan IntervalTimeSpan
    {
        get
        {
            if (string.IsNullOrEmpty(Interval))
            {
                return TimeSpan.Zero;
            }
            return System.Xml.XmlConvert.ToTimeSpan(Interval);
        }
    }
}
