using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;
using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder;

public class UserService(
    UserManager<User> userManager,
    ApplicationDbContext db,
    DiscordService discord,
    IEmailSender emailSender
)
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
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"User creation failed: {errors}");
        }

        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, name));
        user.Preferences.Language = LanguageFlags.English;
        return user;
    }

    public async Task<User> CreateDiscordUser(
        string email,
        string username,
        string discordId,
        string name
    )
    {
        var user = new User();

        await userStore.SetUserNameAsync(user, username, CancellationToken.None);
        await userStore.SetEmailConfirmedAsync(user, true);
        await userStore.SetEmailAsync(user, email, CancellationToken.None);
        var result = await userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            throw new Exception("User creation failed");
        }

        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, name));
        await userManager.AddClaimAsync(
            user,
            new Claim(ApplicationClaimTypes.DiscordId, discordId)
        );
        user.Preferences.Language = LanguageFlags.English;
        return user;
    }

    public async Task SetDiscordToken(User user, string token)
    {
        await userManager.AddClaimAsync(user, new Claim(ApplicationClaimTypes.DiscordToken, token));
    }

    public async Task<string> GetDiscordToken(User user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        return claims.First(c => c.Type == ApplicationClaimTypes.DiscordToken).Value;
    }

    public async Task<List<DiscordServer>> GetUserDiscordServers(User user)
    {
        var discordToken = await GetDiscordToken(user);

        if (string.IsNullOrEmpty(discordToken))
        {
            throw new Exception("Discord token not found for user");
        }

        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://discord.com/api/users/@me/guilds"
        );
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            discordToken
        );

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch Discord servers: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var servers =
            System.Text.Json.JsonSerializer.Deserialize<List<DiscordServer>>(content)
            ?? throw new Exception("Failed to parse Discord servers");

        return servers;
    }

    public class DiscordServer
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("icon")]
        public required string Icon { get; set; }

        public string IconUrl => $"https://cdn.discordapp.com/icons/{Id}/{Icon}.png";
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
        var discordIdClaim =
            c.Find(c => c.Type == ApplicationClaimTypes.DiscordId)
            ?? throw new Exception("User already exists without Discord ID");
        if (discordIdClaim.Value != discordId)
        {
            throw new Exception("Discord ID mismatch");
        }
        else
        {
            return user;
        }
    }

    public async Task<ulong?> GetDiscordId(string userId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            return null;
        }
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims) ?? throw new Exception("User claims not found");
        var discordIdClaim = c.Find(c => c.Type == ApplicationClaimTypes.DiscordId);
        if (discordIdClaim == null)
        {
            return null;
        }

        return ulong.Parse(discordIdClaim.Value);
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
            await userManager.ReplaceClaimAsync(
                user,
                oldClaim,
                new Claim(ClaimTypes.Name, newName)
            );
            return true;
        }

        return false;
    }

    public async Task<bool> NotifyUser(User user, string message)
    {
        var logins = await userManager.GetLoginsAsync(user);
        var claims = await userManager.GetClaimsAsync(user);
        var discordIdClaim = claims.FirstOrDefault(c => c.Type == ApplicationClaimTypes.DiscordId);
        var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

        if (discordIdClaim != null)
        {
            discord.QueueSendDM(ulong.Parse(discordIdClaim.Value), message);
        }
        else if (emailClaim != null)
        {
            await emailSender.SendEmailAsync(emailClaim.Value, "Group Found", message);
        }
        else
        {
            Console.WriteLine($"Could not notify user ${user.UserName} ({user.Id})");
            return false;
        }

        return true;
    }
}
