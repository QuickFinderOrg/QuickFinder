using Coravel.Events.Interfaces;

namespace QuickFinder;

public class UserDeleted(string id, string name) : IEvent
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
}
