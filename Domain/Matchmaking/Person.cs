using Microsoft.EntityFrameworkCore;

namespace group_finder.Domain.Matchmaking;

public class Person()
{
    public Guid Id { get; init; }
    public required Guid UserId { get; set; }
    public required Criteria Criteria { get; set; }
    public required Preferences Preferences { get; set; }
}

[Owned]
public class Criteria()
{
    public required Availability Availability { get; set; }
    public required string Language = "English";
}

[Owned]
public class Preferences()
{

}

public enum Availability
{
    Daytime,
    Afternoons
}