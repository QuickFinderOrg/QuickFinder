using System.Security.Claims;
using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Humanizer;
using Mailjet.Client;
using Mailjet.Client.Resources;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace group_finder;

public class UserService(UserManager<User> userManager, ApplicationDbContext db, DiscordBotService discord, IEmailSender emailSender, IConfiguration configuration)
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

    public async Task<User> CreateDiscordUser(string email, string name, string discordId)
    {
        var user = new User();

        await userStore.SetUserNameAsync(user, email, CancellationToken.None);
        await userStore.SetEmailConfirmedAsync(user, true);
        await userStore.SetEmailAsync(user, email, CancellationToken.None);
        var result = await userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            throw new Exception("User creation failed");
        }

        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, name));
        await userManager.AddClaimAsync(user, new Claim("DiscordId", discordId));
        return user;
    }

    public async Task<User> GetUser(string userId)
    {
        return await db.Users.FindAsync(userId) ?? throw new Exception("User not found!");
    }

    public async Task<User?> GetUserByDiscordId(string email, string discordId)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return null;
        }
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims) ?? throw new Exception("User claims not found");
        var discordIdClaim = c.Find(c => c.Type == "DiscordId") ?? throw new Exception("User already exists without Discord ID");
        if (discordIdClaim.Value != discordId)
        {
            throw new Exception("Discord ID mismatch");
        }
        else
        {
            return user;
        }
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
        var logins = await userManager.GetLoginsAsync(user);
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);

        var discordIdClaim = c.Find(c => c.Type == ClaimTypes.NameIdentifier);
        var sendToEmailClaim = c.Find(c => c.Type == ClaimTypes.Email);
        if (discordIdClaim != null)
        {
            await discord.SendDM(ulong.Parse(discordIdClaim.Value), message);


        }
        else if (sendToEmailClaim != null)
        {
            await emailSender.SendEmailAsync(sendToEmailClaim.Value, "Group Found", message);
        }
        else
        {
            Console.WriteLine($"Could not notify user ${user.UserName} ({user.Id})");
            return false;
        }


        return true;
    }
}

public class NotifyUsersOnGroupFilled(UserService userService, DiscordBotService discordBot, Logger<NotifyUsersOnGroupFilled> logger) : INotificationHandler<GroupFilled>
{
    public async Task Handle(GroupFilled notification, CancellationToken cancellationToken)
    {
        var names = await Task.WhenAll(notification.Group.Members.Select(userService.GetName));
        var name_list = string.Join("", names.Select(name => $"- {name}(ID)\n"));
        try
        {
            var new_channel_id = discordBot.CreateChannel(notification.Group.Id.ToString());
        }
        catch (System.Exception e)
        {
            logger.LogError(e, "Error creating Discord channel for group {GroupId}", notification.Group.Id);
        }

        await Task.WhenAll(notification.Group.Members.Select(user => userService.NotifyUser(user, $"Group found for {notification.Group.Course.Name}.\n Your members: \n{name_list}")));
    }
}