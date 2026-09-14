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
    }
}
