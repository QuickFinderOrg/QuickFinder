namespace group_finder;

public record class UserToBeDeleted : BaseDomainEvent
{
    public required User User;
}
