using System.ComponentModel.DataAnnotations.Schema;

namespace QuickFinder;

public abstract class BaseEntity
{
    [NotMapped]
    public List<BaseDomainEvent> Events = [];
}
