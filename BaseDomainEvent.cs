using MediatR;

namespace QuickFinder;

public abstract record BaseDomainEvent : INotification
{
    public DateTimeOffset DateOccurred { get; protected set; } = DateTimeOffset.UtcNow;
}
