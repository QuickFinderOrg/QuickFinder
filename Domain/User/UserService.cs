using System.Security.Claims;
using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace group_finder;

public class UserService(UserManager<User> userManager, ApplicationDbContext db, DiscordBotService discord)
{
    private readonly UserStore<User> userStore = new UserStore<User>(db);

    public bool HasUsers()
    {
        return userStore.Users.Any();
    }

    public async Task<User> CreateUser(string email, string name, string password)
    {
        var user = new User();

        await userStore.SetUserNameAsync(user, email, CancellationToken.None);
        await userStore.SetEmailConfirmedAsync(user, true);
        await userStore.SetEmailAsync(user, email, CancellationToken.None);
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new Exception("User creation failed");
        }


        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, name));

        return user;



    }

    public async Task<User[]> GetAllUsers()
    {
        return await userStore.Users.ToArrayAsync();
    }

    public async Task<string> GetName(User user)
    {
        if (user == null)
        {
            return "User not found";
        }
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);

        if (c == null)
        {
            return "User claims not found";
        }

        var nameClaim = c.Find(c => c.Type == ClaimTypes.Name);

        if (nameClaim == null)
        {
            return "User name not found";
        }

        return nameClaim.Value;
    }
    public async Task<Claim?> GetNameClaim(User user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);

        var nameClaim = c.Find(c => c.Type == ClaimTypes.Name);

        return nameClaim;
    }

    public async Task<bool> UpdateName(User user, string newName)
    {
        var oldClaim = await GetNameClaim(user);

        if (oldClaim == null)
        {
            await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, newName));
            return true;
        }

        if (oldClaim.Value != newName)
        {
            await userManager.ReplaceClaimAsync(user, oldClaim, new Claim(ClaimTypes.Name, newName));
            return true;
        }

        return false;
    }

    public async Task<bool> NotifyUser(User user, string message)
    {
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);

        var discordIdClaim = c.Find(c => c.Type == ClaimTypes.NameIdentifier);

        if (discordIdClaim == null)
        {
            // might be a test user or someone without a discord attached.
            return false;
        }

        await discord.SendDM(ulong.Parse(discordIdClaim.Value), message);
        return true;
    }
}

public class OnGroupFilled(UserService userService) : INotificationHandler<GroupFilled>
{
    public async Task Handle(GroupFilled notification, CancellationToken cancellationToken)
    {
        var names = await Task.WhenAll(notification.Group.Members.Select(userService.GetName));
        var name_list = string.Join("", names.Select(name => $"- {name}(ID)\n"));
        await Task.WhenAll(notification.Group.Members.Select(user => userService.NotifyUser(user, $"Group found for {notification.Group.Course.Name}.\n Your members: \n{name_list}")));
    }
}