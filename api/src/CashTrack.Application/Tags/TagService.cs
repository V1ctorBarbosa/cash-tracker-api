using CashTrack.Application.Budgets;
using CashTrack.Domain.Common;
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
        var tags = await EnsureUncategorizedTagsExistAsync(userId, cancellationToken);
        return tags.Select(ToDto).ToList();
    }

    public async Task<TagDto> CreateAsync(UserId userId, CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        var tag = Tag.Create(
            new TagId(NewEntityId.Generate()),
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

        if (existingTag.IsSystemDefined)
        {
            throw new ArgumentException("The uncategorized tag cannot be edited.", nameof(tagId));
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

        if (existingTag.IsSystemDefined)
        {
            throw new ArgumentException("The uncategorized tag cannot be deleted.", nameof(tagId));
        }

        await _tagRepository.DeleteAsync(userId, tagId, cancellationToken);
        await _budgetService.DeleteAllForTagAsync(userId, tagId, cancellationToken);
        return true;
    }

    /// <summary>Lazily provisions the user's "uncategorized" tag for each transaction kind that doesn't have one yet, mirroring RecurringTransactionMaterializationService's check-then-create-if-missing pattern (Section 12) -- a deterministic id makes re-creation after deletion harmless.</summary>
    private async Task<IReadOnlyList<Tag>> EnsureUncategorizedTagsExistAsync(UserId userId, CancellationToken cancellationToken)
    {
        var tags = await _tagRepository.ListByUserAsync(userId, cancellationToken);
        var kindsWithAnUncategorizedTag = tags.Where(tag => tag.IsSystemDefined).Select(tag => tag.Kind).ToHashSet();

        var missingUncategorizedTags = Enum.GetValues<TransactionType>()
            .Where(kind => !kindsWithAnUncategorizedTag.Contains(kind))
            .Select(kind => Tag.CreateUncategorized(userId, kind))
            .ToList();

        if (missingUncategorizedTags.Count == 0)
        {
            return tags;
        }

        foreach (var uncategorizedTag in missingUncategorizedTags)
        {
            await _tagRepository.SaveAsync(uncategorizedTag, cancellationToken);
        }

        return [.. tags, .. missingUncategorizedTags];
    }

    private static TagDto ToDto(Tag tag) => new(tag.Id.Value, tag.Name, tag.Color.Value, tag.Kind.ToString(), tag.IsSystemDefined);
}
