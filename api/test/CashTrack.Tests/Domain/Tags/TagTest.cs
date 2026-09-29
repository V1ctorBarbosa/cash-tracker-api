using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
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
        var tag = Tag.Create(id, userId, "Mercado", color, TransactionType.Expense);

        // Assert
        tag.Id.Should().Be(id);
        tag.UserId.Should().Be(userId);
        tag.Name.Should().Be("Mercado");
        tag.Color.Should().Be(color);
        tag.Kind.Should().Be(TransactionType.Expense);
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

    [Fact]
    public void Create_IsNotSystemDefined()
    {
        // Act
        var tag = CreateValidTag();

        // Assert
        tag.IsSystemDefined.Should().BeFalse();
    }

    [Theory]
    [InlineData(TransactionType.Expense)]
    [InlineData(TransactionType.Income)]
    public void CreateUncategorized_ReturnsSystemDefinedTag_WithDeterministicIdForKind(TransactionType kind)
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");

        // Act
        var tag = Tag.CreateUncategorized(userId, kind);

        // Assert
        tag.Id.Should().Be(TagId.ForUncategorized(userId, kind));
        tag.UserId.Should().Be(userId);
        tag.Name.Should().Be("Não categorizado");
        tag.Kind.Should().Be(kind);
        tag.IsSystemDefined.Should().BeTrue();
    }

    [Fact]
    public void CreateUncategorized_UsesTheSameFixedColor_RegardlessOfKind()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");

        // Act
        var expenseTag = Tag.CreateUncategorized(userId, TransactionType.Expense);
        var incomeTag = Tag.CreateUncategorized(userId, TransactionType.Income);

        // Assert
        expenseTag.Color.Should().Be(incomeTag.Color);
    }

    private static Tag CreateValidTag(string name = "Mercado") => Tag.Create(
        new TagId("tag-mercado"),
        new UserId("firebase-uid-123"),
        name,
        new TagColor("#3B82F6"),
        TransactionType.Expense);
}
