using group_finder.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace group_finder;

public class UserService(UserManager<User> userManager, ApplicationDbContext db)
{
    private readonly UserStore<User> userStore = new UserStore<User>(db);

    public async Task<User> CreateUser()
    {
        var user = new User();

        await userStore.SetUserNameAsync(user, "dr.acula@bloodbank.us", CancellationToken.None);
        await userStore.SetEmailConfirmedAsync(user, true);
        await userStore.SetEmailAsync(user, "dr.acula@bloodbank.us", CancellationToken.None);
        var result = await userManager.CreateAsync(user, "Hema_Globin42");

        if (result.Succeeded)
        {
            return user;
        }
        else
        {
            throw new Exception("User creation failed");
        }


    }
}