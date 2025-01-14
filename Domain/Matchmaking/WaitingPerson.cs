using Microsoft.EntityFrameworkCore;

namespace group_finder.Domain.Matchmaking;

public class WaitingPerson()
{
    public Guid Id { get; init; }
    public required Person Person { get; set; }
}
