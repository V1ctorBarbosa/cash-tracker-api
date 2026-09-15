using CashTrack.Application.Budgets;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Tags;

public sealed class TagService
{
    private readonly ITagRepository _tagRepository;
    private readonly BudgetService _budgetService;

    public TagService(ITagRepository tagRepository, BudgetService budgetService)
    {
        _tagRepository = tagRepository;
        _budgetService = budgetService;
    }

    public async Task<IReadOnlyList<TagDto>> ListAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var tags = await _tagRepository.ListByUserAsync(userId, cancellationToken);
        return tags.Select(ToDto).ToList();
    }

    public async Task<TagDto> CreateAsync(UserId userId, CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        var tag = Tag.Create(
            new TagId(Guid.NewGuid().ToString()),
            userId,
            request.Name,
            new TagColor(request.Color),
            Enum.Parse<TransactionType>(request.Kind, ignoreCase: true));

        await _tagRepository.SaveAsync(tag, cancellationToken);
        return ToDto(tag);
    }

    public async Task<TagDto?> UpdateAsync(
        UserId userId,
        TagId tagId,
        UpdateTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingTag = await _tagRepository.GetByIdAsync(userId, tagId, cancellationToken);
        if (existingTag is null)
        {
            return null;
        }

        var updatedTag = Tag.Create(
            tagId,
            userId,
            request.Name,
            new TagColor(request.Color),
            Enum.Parse<TransactionType>(request.Kind, ignoreCase: true));

        await _tagRepository.SaveAsync(updatedTag, cancellationToken);
        return ToDto(updatedTag);
    }

    public async Task<bool> DeleteAsync(UserId userId, TagId tagId, CancellationToken cancellationToken = default)
    {
        var existingTag = await _tagRepository.GetByIdAsync(userId, tagId, cancellationToken);
        if (existingTag is null)
        {
            return false;
        }

        await _tagRepository.DeleteAsync(userId, tagId, cancellationToken);
        await _budgetService.DeleteAllForTagAsync(userId, tagId, cancellationToken);
        return true;
    }

    private static TagDto ToDto(Tag tag) => new(tag.Id.Value, tag.Name, tag.Color.Value, tag.Kind.ToString());
}
