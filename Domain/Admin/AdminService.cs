using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace group_finder;

public class AdminService(UserManager<User> userManager)
{
    public async Task MakeTeacher(User user)
    {
        await userManager.AddClaimAsync(user, new Claim("IsTeacher", true.ToString()));      
    }

    public async Task RemoveTeacher(User user)
    {
        await userManager.RemoveClaimAsync(user, new Claim("IsTeacher", true.ToString()));
    }
}