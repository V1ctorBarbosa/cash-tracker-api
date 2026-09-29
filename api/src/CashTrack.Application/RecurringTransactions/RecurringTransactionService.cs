using CashTrack.Application.Tags;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.RecurringTransactions;

public sealed class RecurringTransactionService
{
    private const bool ActiveOnCreate = true;

    private readonly IRecurringTransactionRepository _recurringTransactionRepository;
    private readonly ITagRepository _tagRepository;

    public RecurringTransactionService(IRecurringTransactionRepository recurringTransactionRepository, ITagRepository tagRepository)
    {
        _recurringTransactionRepository = recurringTransactionRepository;
        _tagRepository = tagRepository;
    }

    public async Task<IReadOnlyList<RecurringTransactionDto>> ListAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var recurringTransactions = await _recurringTransactionRepository.ListByUserAsync(userId, cancellationToken);
        return recurringTransactions.Select(ToDto).ToList();
    }

    public async Task<RecurringTransactionDto> CreateAsync(
        UserId userId,
        CreateRecurringTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tagId = new TagId(request.TagId);
        await EnsureDescriptionIsProvidedForUncategorizedTagAsync(userId, tagId, request.Description, cancellationToken);

        var recurringTransaction = RecurringTransaction.Create(
            new RecurringTransactionId(NewEntityId.Generate()),
            userId,
            Enum.Parse<TransactionType>(request.Type, ignoreCase: true),
            new Money(request.Amount),
            request.Description,
            tagId,
            Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
            request.DayOfMonth,
            request.StartDate,
            request.EndDate,
            ActiveOnCreate);

        await _recurringTransactionRepository.SaveAsync(recurringTransaction, cancellationToken);
        return ToDto(recurringTransaction);
    }

    public async Task<RecurringTransactionDto?> UpdateAsync(
        UserId userId,
        RecurringTransactionId recurringTransactionId,
        UpdateRecurringTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingRecurringTransaction = await _recurringTransactionRepository.GetByIdAsync(userId, recurringTransactionId, cancellationToken);
        if (existingRecurringTransaction is null)
        {
            return null;
        }

        var tagId = new TagId(request.TagId);
        await EnsureDescriptionIsProvidedForUncategorizedTagAsync(userId, tagId, request.Description, cancellationToken);

        var updatedRecurringTransaction = RecurringTransaction.Create(
            recurringTransactionId,
            userId,
            Enum.Parse<TransactionType>(request.Type, ignoreCase: true),
            new Money(request.Amount),
            request.Description,
            tagId,
            Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
            request.DayOfMonth,
            request.StartDate,
            request.EndDate,
            request.Active,
            existingRecurringTransaction.SkippedMonths);

        await _recurringTransactionRepository.SaveAsync(updatedRecurringTransaction, cancellationToken);
        return ToDto(updatedRecurringTransaction);
    }

    public async Task<bool> DeleteAsync(UserId userId, RecurringTransactionId recurringTransactionId, CancellationToken cancellationToken = default)
    {
        var existingRecurringTransaction = await _recurringTransactionRepository.GetByIdAsync(userId, recurringTransactionId, cancellationToken);
        if (existingRecurringTransaction is null)
        {
            return false;
        }

        await _recurringTransactionRepository.DeleteAsync(userId, recurringTransactionId, cancellationToken);
        return true;
    }

    /// <summary>
    /// Marks a single month as explicitly excluded from materialization, so a deleted occurrence
    /// does not silently reappear the next time a bounded-range query re-materializes this recurrence.
    /// A no-op (not an error) when the recurrence no longer exists -- deleting its last remaining
    /// materialized occurrence is a valid, harmless race with deleting the recurrence itself.
    /// </summary>
    public async Task SkipMonthAsync(UserId userId, RecurringTransactionId recurringTransactionId, YearMonth month, CancellationToken cancellationToken = default)
    {
        var existingRecurringTransaction = await _recurringTransactionRepository.GetByIdAsync(userId, recurringTransactionId, cancellationToken);
        if (existingRecurringTransaction is null)
        {
            return;
        }

        var updatedRecurringTransaction = existingRecurringTransaction.SkipMonth(month);
        await _recurringTransactionRepository.SaveAsync(updatedRecurringTransaction, cancellationToken);
    }

    /// <summary>
    /// A transaction left without a real category must be identifiable some other way -- so
    /// skipping the tag (landing on the system "uncategorized" tag) makes a description mandatory.
    /// A tag id that doesn't resolve to any tag is left alone here; that's an unrelated concern.
    /// </summary>
    private async Task EnsureDescriptionIsProvidedForUncategorizedTagAsync(
        UserId userId,
        TagId tagId,
        string? description,
        CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(userId, tagId, cancellationToken);
        if (tag is { IsSystemDefined: true } && string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required for a transaction using the uncategorized tag.", nameof(description));
        }
    }

    private static RecurringTransactionDto ToDto(RecurringTransaction recurringTransaction) => new(
        recurringTransaction.Id.Value,
        recurringTransaction.Type.ToString(),
        recurringTransaction.Amount.Amount,
        recurringTransaction.Description,
        recurringTransaction.TagId.Value,
        recurringTransaction.PaymentMethod.ToString(),
        recurringTransaction.DayOfMonth,
        recurringTransaction.StartDate,
        recurringTransaction.EndDate,
        recurringTransaction.Active);
}
