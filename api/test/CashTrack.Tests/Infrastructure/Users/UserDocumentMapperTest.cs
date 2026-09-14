using CashTrack.Domain.Users;
using CashTrack.Infrastructure.Users;
using FluentAssertions;

namespace CashTrack.Tests.Infrastructure.Users;

public sealed class UserDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromUser()
    {
        // Arrange
        var user = User.Create(new UserId("firebase-uid-123"), "Victor", "vicfer2708@gmail.com");

        // Act
        var document = UserDocumentMapper.ToDocument(user);

        // Assert
        document.Name.Should().Be("Victor");
        document.Email.Should().Be("vicfer2708@gmail.com");
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = new UserId("firebase-uid-123");
        var originalUser = User.Create(id, "Victor", "vicfer2708@gmail.com");

        // Act
        var document = UserDocumentMapper.ToDocument(originalUser);
        var roundTrippedUser = UserDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedUser.Id.Should().Be(id);
        roundTrippedUser.Name.Should().Be(originalUser.Name);
        roundTrippedUser.Email.Should().Be(originalUser.Email);
    }
}
