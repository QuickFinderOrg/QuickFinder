namespace QuickFinder.Domain.Matchmaking;

public record class UserEventVM
{
    public required string Id;
    public required string Name;
}

public record class GroupEventVM
{
    public required Guid Id;
    public required string Name;
}

public record class CourseEventVM
{
    public required Guid Id;
    public required string Name;
}

public class GroupMemberAdded : BaseDomainEvent
{
    public GroupMemberAdded(User user, Group group)
    {
        User = new UserEventVM { Id = user.Id, Name = user.UserName ?? "" };
        Group = new GroupEventVM { Id = group.Id, Name = group.Name };
    }

    public UserEventVM User;
    public GroupEventVM Group;
}

public class GroupMemberLeft : BaseDomainEvent
{
    public GroupMemberLeft(User user, Group group)
    {
        User = new UserEventVM { Id = user.Id, Name = user.UserName ?? "" };
        Group = new GroupEventVM { Id = group.Id, Name = group.Name };
    }

    public UserEventVM User;
    public GroupEventVM Group;
}

public class GroupDisbanded : BaseDomainEvent
{
    public GroupDisbanded(Group group, IEnumerable<User> members)
    {
        Group = new GroupEventVM { Id = group.Id, Name = group.Name };
        FormerGroupMembers = members
            .Select(m => new UserEventVM { Id = m.Id, Name = m.UserName ?? "" })
            .ToArray();
    }

    public GroupEventVM Group;
    public UserEventVM[] FormerGroupMembers;
}

/// <summary>
/// Fired only the first time a group is filled
/// </summary>
public class GroupFilled : BaseDomainEvent
{
    public GroupFilled(Group group)
    {
        Group = new GroupEventVM { Id = group.Id, Name = group.Name };
    }

    public GroupEventVM Group;
}

public class GroupEmpty : BaseDomainEvent
{
    public GroupEmpty(Group group)
    {
        Group = new GroupEventVM { Id = group.Id, Name = group.Name };
    }

    public GroupEventVM Group;
}

public class CourseJoined : BaseDomainEvent
{
    public CourseJoined(User user, Course course)
    {
        User = new UserEventVM { Id = user.Id, Name = user.UserName ?? "" };
        Course = new CourseEventVM { Id = course.Id, Name = course.Name };
    }

    public UserEventVM User;
    public CourseEventVM Course;
}
