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

    public async Task<bool> IsTeacher(User user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);
        var isTeacher = c.Find(c => c.Type == "IsTeacher");
        if (isTeacher is not null)
        {
            return true;
        }
        
        return false;
    }

    public async Task<bool> IsAdmin(User user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        var c = new List<Claim>(claims);
        var isAdmin = c.Find(c => c.Type == "IsAdmin");
        if (isAdmin is not null)
        {
            return true;
        }
        
        return false;
    }
}