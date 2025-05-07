using Coravel.Events.Interfaces;

namespace QuickFinder;

public abstract class BaseDomainEvent : IEvent
{
    public DateTimeOffset DateOccurred { get; protected set; } = DateTimeOffset.UtcNow;
}
