
namespace group_finder;

public static class UserExtensions
{

    public static void SetUserId(this HttpContext context, string userId)
    {
        context.Session.SetString(Constants.UserSessionKey, userId);
    }

    public static string? GetUserId(this HttpContext context)
    {
        var userId_override = context.RequestServices.GetRequiredService<IConfiguration>()["UserIdOverride"];

        if (!string.IsNullOrWhiteSpace(userId_override))
        {
            Console.WriteLine($"UserId override: {userId_override}");
            return userId_override;
        }

        return context.Session.GetString(Constants.UserSessionKey);
    }

    public static string RequireUserId(this HttpContext context)
    {
        return context.Session.GetString(Constants.UserSessionKey) ?? throw new Exception("ID not found in session");
    }

    public static void ClearUserId(this HttpContext context)
    {
        context.Session.Remove(Constants.UserSessionKey);
    }
}