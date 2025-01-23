using Microsoft.EntityFrameworkCore;

namespace group_finder.Domain.Matchmaking;

public class Person()
{
    public Guid Id { get; init; }
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
    public required Criteria Criteria { get; set; }
    public required Preferences Preferences { get; set; }
}

[Owned]
public record class Criteria()
{
    public Availability Availability { get; set; } = Availability.Daytime;
    public string Language = "English";
}

[Owned]
public record class Preferences()
{

}

public enum Availability
{
    Daytime,
    Afternoons
}