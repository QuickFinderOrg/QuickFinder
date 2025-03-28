namespace QuickFinder;

public record class UserToBeDeleted : BaseDomainEvent
{
    public required User User;
}
