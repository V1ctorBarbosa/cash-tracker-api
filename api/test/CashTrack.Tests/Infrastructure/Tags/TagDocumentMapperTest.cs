using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using CashTrack.Infrastructure.Tags;
using FluentAssertions;

namespace CashTrack.Tests.Infrastructure.Tags;

public sealed class TagDocumentMapperTest
{
    [Fact]
    public void ToDocument_MapsAllFields_FromTag()
    {
        // Arrange
        var tag = Tag.Create(
            new TagId("tag-mercado"),
            new UserId("firebase-uid-123"),
            "Mercado",
            new TagColor("#3B82F6"),
            TransactionType.Expense);

        // Act
        var document = TagDocumentMapper.ToDocument(tag);

        // Assert
        document.UserId.Should().Be("firebase-uid-123");
        document.Name.Should().Be("Mercado");
        document.Color.Should().Be("#3B82F6");
        document.Kind.Should().Be("Expense");
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument()
    {
        // Arrange
        var id = new TagId("tag-mercado");
        var originalTag = Tag.Create(id, new UserId("firebase-uid-123"), "Mercado", new TagColor("#3B82F6"), TransactionType.Income);

        // Act
        var document = TagDocumentMapper.ToDocument(originalTag);
        var roundTrippedTag = TagDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedTag.Id.Should().Be(id);
        roundTrippedTag.UserId.Should().Be(originalTag.UserId);
        roundTrippedTag.Name.Should().Be(originalTag.Name);
        roundTrippedTag.Color.Should().Be(originalTag.Color);
        roundTrippedTag.Kind.Should().Be(originalTag.Kind);
        roundTrippedTag.IsSystemDefined.Should().BeFalse();
    }

    [Fact]
    public void ToDocument_MapsIsSystemDefined_ForAnUncategorizedTag()
    {
        // Arrange
        var userId = new UserId("firebase-uid-123");
        var tag = Tag.CreateUncategorized(userId, TransactionType.Expense);

        // Act
        var document = TagDocumentMapper.ToDocument(tag);

        // Assert
        document.IsSystemDefined.Should().BeTrue();
    }

    [Fact]
    public void ToDomain_RoundTrips_ThroughToDocument_ForAnUncategorizedTag()
    {
        // Arrange -- name/color/id are regenerated from code, not trusted from storage, so the
        // stored document intentionally carries different values than what should come back out.
        var userId = new UserId("firebase-uid-123");
        var id = TagId.ForUncategorized(userId, TransactionType.Expense);
        var originalTag = Tag.CreateUncategorized(userId, TransactionType.Expense);

        // Act
        var document = TagDocumentMapper.ToDocument(originalTag);
        var roundTrippedTag = TagDocumentMapper.ToDomain(id, document);

        // Assert
        roundTrippedTag.Id.Should().Be(id);
        roundTrippedTag.Name.Should().Be(originalTag.Name);
        roundTrippedTag.Color.Should().Be(originalTag.Color);
        roundTrippedTag.Kind.Should().Be(TransactionType.Expense);
        roundTrippedTag.IsSystemDefined.Should().BeTrue();
    }
}
