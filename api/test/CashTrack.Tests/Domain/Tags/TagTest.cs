using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Tags;

public sealed class TagTest
{
    [Fact]
    public void Create_ReturnsTag_WithGivenValues()
    {
        // Arrange
        var id = new TagId("tag-mercado");
        var userId = new UserId("firebase-uid-123");
        var color = new TagColor("#3B82F6");

        // Act
        var tag = Tag.Create(id, userId, "Mercado", color);

        // Assert
        tag.Id.Should().Be(id);
        tag.UserId.Should().Be(userId);
        tag.Name.Should().Be("Mercado");
        tag.Color.Should().Be(color);
    }

    [Fact]
    public void Create_TrimsName()
    {
        // Act
        var tag = CreateValidTag(name: "  Mercado  ");

        // Assert
        tag.Name.Should().Be("Mercado");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsEmptyOrWhitespace(string name)
    {
        // Act
        var act = () => CreateValidTag(name: name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static Tag CreateValidTag(string name = "Mercado") => Tag.Create(
        new TagId("tag-mercado"),
        new UserId("firebase-uid-123"),
        name,
        new TagColor("#3B82F6"));
}
