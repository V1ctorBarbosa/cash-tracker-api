using CashTrack.Application.Users;
using CashTrack.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace CashTrack.Tests.Application.Users;

public sealed class UserProfileServiceTest
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly UserProfileService _sut;

    public UserProfileServiceTest()
    {
        _sut = new UserProfileService(_userRepository);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsMappedDto_WhenUserExists()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var user = User.Create(userId, "Victor", "vicfer2708@gmail.com");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("firebase-uid-123");
        result.Name.Should().Be("Victor");
        result.Email.Should().Be("vicfer2708@gmail.com");
    }

    [Fact]
    public async Task UpsertProfileAsync_SavesUser_WithValuesFromRequest()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var request = new UpsertUserProfileRequest("Victor", "vicfer2708@gmail.com");

        // Act
        var result = await _sut.UpsertProfileAsync(userId, request);

        // Assert
        result.Id.Should().Be("firebase-uid-123");
        result.Name.Should().Be("Victor");
        result.Email.Should().Be("vicfer2708@gmail.com");
        await _userRepository.Received(1).SaveAsync(
            Arg.Is<User>(user => user.Id == userId && user.Name == "Victor"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertProfileAsync_Throws_AndDoesNotSave_WhenNameIsEmpty()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var request = new UpsertUserProfileRequest("", "vicfer2708@gmail.com");

        // Act
        var act = async () => await _sut.UpsertProfileAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _userRepository.DidNotReceive().SaveAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
