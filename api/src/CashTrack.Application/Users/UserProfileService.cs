using CashTrack.Domain.Users;

namespace CashTrack.Application.Users;

public sealed class UserProfileService
{
    private readonly IUserRepository _userRepository;

    public UserProfileService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto?> GetProfileAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return ToDto(user);
    }

    public async Task<UserProfileDto> UpsertProfileAsync(
        UserId userId,
        UpsertUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = User.Create(userId, request.Name, request.Email);

        await _userRepository.SaveAsync(user, cancellationToken);
        return ToDto(user);
    }

    private static UserProfileDto ToDto(User user) => new(user.Id.Value, user.Name, user.Email);
}
