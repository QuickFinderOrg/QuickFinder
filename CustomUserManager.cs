using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace QuickFinder;

public class CustomUserManager(
    IUserStore<User> store, // Replace User with your user type
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<User> passwordHasher, // Replace User with your user type
    IEnumerable<IUserValidator<User>> userValidators, // Replace User with your user type
    IEnumerable<IPasswordValidator<User>> passwordValidators, // Replace User with your user type
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<CustomUserManager> logger,
    IMediator mediator) : UserManager<User>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger) // Replace User with your user type
{
    private readonly ILogger<CustomUserManager> _logger = logger;

    public override async Task<IdentityResult> DeleteAsync(User user) // Replace User with your user type
    {
        // Your custom logic before deletion
        _logger.LogInformation($"User {user.UserName} (ID: {user.Id}) is about to be deleted.");

        // Perform any other actions you need to do before deletion, such as:
        // - Deleting related data from other tables
        // - Sending a notification email
        // - Logging the deletion event

        await mediator.Publish(new UserToBeDeleted() { User = user });

        // Call the base class's DeleteAsync method to actually delete the user
        var result = await base.DeleteAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation($"User {user.UserName} (ID: {user.Id}) has been successfully deleted.");
        }
        else
        {
            _logger.LogError($"Failed to delete user {user.UserName} (ID: {user.Id}). Errors: {string.Join(", ", result.Errors)}");
        }

        return result;
    }
}
