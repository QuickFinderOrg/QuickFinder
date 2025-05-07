using Coravel.Queuing.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace QuickFinder;

public class CustomUserManager(
    IUserStore<User> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<User> passwordHasher,
    IEnumerable<IUserValidator<User>> userValidators,
    IEnumerable<IPasswordValidator<User>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<CustomUserManager> logger,
    IQueue queue
)
    : UserManager<User>(
        store,
        optionsAccessor,
        passwordHasher,
        userValidators,
        passwordValidators,
        keyNormalizer,
        errors,
        services,
        logger
    )
{
    private readonly ILogger<CustomUserManager> _logger = logger;

    public override async Task<IdentityResult> DeleteAsync(User user)
    {
        queue.QueueBroadcast(new UserDeleted(user.Id, "name"));

        var result = await base.DeleteAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                $"User {user.UserName} (ID: {user.Id}) has been successfully deleted."
            );
        }
        else
        {
            _logger.LogError(
                $"Failed to delete user {user.UserName} (ID: {user.Id}). Errors: {string.Join(", ", result.Errors)}"
            );
        }

        return result;
    }
}
