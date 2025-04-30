using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace QuickFinder;

public class AdminService(UserManager<User> userManager)
{
    public async Task MakeTeacher(User user)
    {
        await userManager.AddClaimAsync(
            user,
            new Claim(ApplicationClaimTypes.IsTeacher, true.ToString())
        );
    }

    public async Task RemoveTeacher(User user)
    {
        await userManager.RemoveClaimAsync(
            user,
            new Claim(ApplicationClaimTypes.IsTeacher, true.ToString())
        );
    }

    public async Task<bool> IsTeacher(User user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        return claims.Any(c => c.Type == ApplicationClaimTypes.IsTeacher);
    }

    public async Task<bool> IsAdmin(User user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        return claims.Any(c => c.Type == ApplicationClaimTypes.IsAdmin);
    }
}
