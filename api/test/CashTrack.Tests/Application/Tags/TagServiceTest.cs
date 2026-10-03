using CashTrack.Application.Budgets;
using CashTrack.Application.Tags;
using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace CashTrack.Tests.Application.Tags;

public sealed class TagServiceTest
{
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly TagService _sut;
    private readonly UserId _userId = new("firebase-uid-123");

    public TagServiceTest()
    {
        // No budgets by default, so cascading the delete is a real no-op instead of a mocked
        // one -- BudgetService is sealed and can't be substituted.
        _budgetRepository.ListByUserAsync(_userId, null, Arg.Any<CancellationToken>()).Returns([]);
        var budgetService = new BudgetService(_budgetRepository);

        _sut = new TagService(_tagRepository, budgetService);
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos_FromRepository()
    {
        // Arrange -- both system tags already provisioned, so this exercises mapping only
        var tag = Tag.Create(new TagId("tag-mercado"), _userId, "Mercado", new TagColor("#3B82F6"), TransactionType.Expense);
        _tagRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([
            tag,
            Tag.CreateUncategorized(_userId, TransactionType.Expense),
            Tag.CreateUncategorized(_userId, TransactionType.Income),
        ]);

        // Act
        var result = await _sut.ListAsync(_userId);

        // Assert
        var mercadoDto = result.Single(dto => dto.Id == "tag-mercado");
        mercadoDto.Name.Should().Be("Mercado");
        mercadoDto.Color.Should().Be("#3B82F6");
        mercadoDto.Kind.Should().Be("Expense");
        mercadoDto.IsSystemDefined.Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_ProvisionsBothUncategorizedTags_WhenNeitherExistsYet()
    {
        // Arrange
        _tagRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await _sut.ListAsync(_userId);

        // Assert
        result.Should().Contain(dto => dto.IsSystemDefined && dto.Kind == "Expense");
        result.Should().Contain(dto => dto.IsSystemDefined && dto.Kind == "Income");
        await _tagRepository.Received(1).SaveAsync(
            Arg.Is<Tag>(savedTag => savedTag.IsSystemDefined && savedTag.Kind == TransactionType.Expense), Arg.Any<CancellationToken>());
        await _tagRepository.Received(1).SaveAsync(
            Arg.Is<Tag>(savedTag => savedTag.IsSystemDefined && savedTag.Kind == TransactionType.Income), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_DoesNotReprovision_WhenBothUncategorizedTagsAlreadyExist()
    {
        // Arrange
        _tagRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([
            Tag.CreateUncategorized(_userId, TransactionType.Expense),
            Tag.CreateUncategorized(_userId, TransactionType.Income),
        ]);

        // Act
        await _sut.ListAsync(_userId);

        // Assert
        await _tagRepository.DidNotReceive().SaveAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
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
    public async Task UpdateAsync_Throws_WhenTagIsSystemDefined()
    {
        // Arrange
        var tagId = TagId.ForUncategorized(_userId, TransactionType.Expense);
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(Tag.CreateUncategorized(_userId, TransactionType.Expense));

        // Act
        var act = async () => await _sut.UpdateAsync(_userId, tagId, new UpdateTagRequest("Novo nome", "#3B82F6", "Expense"));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _tagRepository.DidNotReceive().SaveAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenTagIsSystemDefined()
    {
        // Arrange
        var tagId = TagId.ForUncategorized(_userId, TransactionType.Expense);
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(Tag.CreateUncategorized(_userId, TransactionType.Expense));

        // Act
        var act = async () => await _sut.DeleteAsync(_userId, tagId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _tagRepository.DidNotReceive().DeleteAsync(Arg.Any<UserId>(), Arg.Any<TagId>(), Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task DeleteAsync_AlsoDeletesEveryBudget_SetForTheDeletedTag()
    {
        // Arrange -- otherwise a budget is left pointing at a tag that no longer exists, showing
        // up on the dashboard as an unresolvable raw tag id with a nonsensical "100% livre".
        var tagId = new TagId("tag-mercado");
        var existingTag = Tag.Create(tagId, _userId, "Mercado", new TagColor("#3B82F6"), TransactionType.Expense);
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns(existingTag);

        var budgetForThisTag = Budget.Create(_userId, tagId, new YearMonth(2026, 9), new Money(1000m));
        var budgetForAnotherTag = Budget.Create(_userId, new TagId("tag-lazer"), new YearMonth(2026, 9), new Money(200m));
        _budgetRepository.ListByUserAsync(_userId, null, Arg.Any<CancellationToken>()).Returns([budgetForThisTag, budgetForAnotherTag]);

        // Act
        await _sut.DeleteAsync(_userId, tagId);

        // Assert
        await _budgetRepository.Received(1).DeleteAsync(_userId, budgetForThisTag.Id, Arg.Any<CancellationToken>());
        await _budgetRepository.DidNotReceive().DeleteAsync(_userId, budgetForAnotherTag.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DoesNotTouchBudgets_WhenTagDoesNotBelongToUser()
    {
        // Arrange
        var tagId = new TagId("tag-mercado");
        _tagRepository.GetByIdAsync(_userId, tagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        // Act
        await _sut.DeleteAsync(_userId, tagId);

        // Assert
        await _budgetRepository.DidNotReceive().DeleteAsync(Arg.Any<UserId>(), Arg.Any<BudgetId>(), Arg.Any<CancellationToken>());
    }
}
