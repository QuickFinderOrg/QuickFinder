namespace QuickFinder;

public class UserDeleted(string id, string name) : BaseDomainEvent
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
}
