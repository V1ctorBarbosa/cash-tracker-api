using CashTrack.Application.Tags;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace CashTrack.Tests.Application.Tags;

public sealed class TagServiceTest
{
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly TagService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public TagServiceTest()
    {
        _sut = new TagService(_tagRepository);
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos_FromRepository()
    {
        // Arrange
        var tag = Tag.Create(new TagId("tag-mercado"), _userId, "Mercado", new TagColor("#3B82F6"), TransactionType.Expense);
        _tagRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([tag]);

        // Act
        var result = await _sut.ListAsync(_userId);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be("tag-mercado");
        result[0].Name.Should().Be("Mercado");
        result[0].Color.Should().Be("#3B82F6");
        result[0].Kind.Should().Be("Expense");
    }

    [Fact]
    public async Task CreateAsync_SavesTag_OwnedByRequestingUser()
    {
        // Arrange
        var request = new CreateTagRequest("Mercado", "#3B82F6", "Expense");

        // Act
        var result = await _sut.CreateAsync(_userId, request);

        // Assert
        result.Name.Should().Be("Mercado");
        result.Color.Should().Be("#3B82F6");
        result.Kind.Should().Be("Expense");
        await _tagRepository.Received(1).SaveAsync(
            Arg.Is<Tag>(tag => tag.UserId == _userId && tag.Name == "Mercado" && tag.Kind == TransactionType.Expense),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenTagDoesNotBelongToUser()
    {
        // Arrange
        var tagId = new TagId("tag-mercado");
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        // Act
        var result = await _sut.UpdateAsync(_userId, tagId, new UpdateTagRequest("Mercado", "#3B82F6", "Expense"));

        // Assert
        result.Should().BeNull();
        await _tagRepository.DidNotReceive().SaveAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SavesUpdatedTag_WhenTagBelongsToUser()
    {
        // Arrange
        var tagId = new TagId("tag-mercado");
        var existingTag = Tag.Create(tagId, _userId, "Mercado", new TagColor("#3B82F6"), TransactionType.Expense);
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(existingTag);

        // Act
        var result = await _sut.UpdateAsync(_userId, tagId, new UpdateTagRequest("Supermercado", "#EF4444", "Expense"));

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Supermercado");
        result.Color.Should().Be("#EF4444");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenTagDoesNotBelongToUser()
    {
        // Arrange
        var tagId = new TagId("tag-mercado");
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        // Act
        var result = await _sut.DeleteAsync(_userId, tagId);

        // Assert
        result.Should().BeFalse();
        await _tagRepository.DidNotReceive().DeleteAsync(Arg.Any<UserId>(), Arg.Any<TagId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndDeletes_WhenTagBelongsToUser()
    {
        // Arrange
        var tagId = new TagId("tag-mercado");
        var existingTag = Tag.Create(tagId, _userId, "Mercado", new TagColor("#3B82F6"), TransactionType.Expense);
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(existingTag);

        // Act
        var result = await _sut.DeleteAsync(_userId, tagId);

        // Assert
        result.Should().BeTrue();
        await _tagRepository.Received(1).DeleteAsync(_userId, tagId, Arg.Any<CancellationToken>());
    }
}
