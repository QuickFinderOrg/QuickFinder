using System.ComponentModel.DataAnnotations.Schema;

namespace group_finder;

public abstract class BaseEntity
{
    [NotMapped]
    public List<BaseDomainEvent> Events = [];
}
