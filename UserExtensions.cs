
namespace group_finder;

public static class UserExtensions
{

    public static void SetUserId(this HttpContext context, int userId)
    {
        context.Session.SetInt32(Constants.UserSessionKey, userId);
    }

    public static int? GetUserId(this HttpContext context)
    {
        var userId_override = context.RequestServices.GetRequiredService<IConfiguration>()["UserIdOverride"];

        if (!string.IsNullOrWhiteSpace(userId_override))
        {
            Console.WriteLine($"UserId override: {userId_override}");
            return int.Parse(userId_override);
        }

        return context.Session.GetInt32(Constants.UserSessionKey);
    }

    public static int RequireUserId(this HttpContext context)
    {
        return context.Session.GetInt32(Constants.UserSessionKey) ?? throw new Exception("ID not found in session");
    }

    public static void ClearUserId(this HttpContext context)
    {
        context.Session.Remove(Constants.UserSessionKey);
    }
}