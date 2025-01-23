using System.Security.Claims;
using group_finder.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace group_finder;

public class UserService(UserManager<User> userManager, ApplicationDbContext db)
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

    public async Task<string> GetName(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);

        return c.Find(c => c.Type == ClaimTypes.Name).Value;
    }
}