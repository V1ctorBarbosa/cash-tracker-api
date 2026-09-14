using CashTrack.Domain.Users;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Users;

public sealed class UserTest
{
    [Fact]
    public void Create_ReturnsUser_WithGivenValues()
    {
        // Arrange
        var id = new UserId("firebase-uid-123");

        // Act
        var user = User.Create(id, "Victor", "vicfer2708@gmail.com");

        // Assert
        user.Id.Should().Be(id);
        user.Name.Should().Be("Victor");
        user.Email.Should().Be("vicfer2708@gmail.com");
    }

    [Fact]
    public void Create_TrimsNameAndEmail()
    {
        // Act
        var user = CreateValidUser(name: "  Victor  ", email: "  vicfer2708@gmail.com  ");

        // Assert
        user.Name.Should().Be("Victor");
        user.Email.Should().Be("vicfer2708@gmail.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsEmptyOrWhitespace(string name)
    {
        // Act
        var act = () => CreateValidUser(name: name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenEmailIsEmptyOrWhitespace(string email)
    {
        // Act
        var act = () => CreateValidUser(email: email);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static User CreateValidUser(string name = "Victor", string email = "vicfer2708@gmail.com") =>
        User.Create(new UserId("firebase-uid-123"), name, email);
}
