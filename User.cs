using Microsoft.AspNetCore.Identity;

namespace group_finder;

public class User : IdentityUser
{
    public string StudentAvailability = "daytime";
}