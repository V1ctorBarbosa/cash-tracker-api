using CashTrack.Application.RecurringTransactions;
using CashTrack.Application.Tags;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Transactions;

public sealed class TransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITagRepository _tagRepository;
    private readonly RecurringTransactionMaterializationService _recurringTransactionMaterializationService;
    private readonly RecurringTransactionService _recurringTransactionService;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ITagRepository tagRepository,
        RecurringTransactionMaterializationService recurringTransactionMaterializationService,
        RecurringTransactionService recurringTransactionService)
    {
        _transactionRepository = transactionRepository;
        _tagRepository = tagRepository;
        _recurringTransactionMaterializationService = recurringTransactionMaterializationService;
        _recurringTransactionService = recurringTransactionService;
    }

    public async Task<IReadOnlyList<TransactionDto>> ListAsync(
        UserId userId,
        DateOnly? from,
        DateOnly? to,
        string? tagId,
        string? type,
        string? paymentMethod,
        CancellationToken cancellationToken = default)
    {
        // Recurring transactions are only materialized for a bounded range (Section 12) -- an
        // unbounded list has no "current month" to generate against.
        if (from is { } boundedFrom && to is { } boundedTo)
        {
            await _recurringTransactionMaterializationService.MaterializeAsync(userId, boundedFrom, boundedTo, cancellationToken);
        }

        var query = new TransactionQuery(
            from,
            to,
            tagId is null ? null : new TagId(tagId),
            type is null ? null : Enum.Parse<TransactionType>(type, ignoreCase: true),
            paymentMethod is null ? null : Enum.Parse<PaymentMethod>(paymentMethod, ignoreCase: true));

        var transactions = await _transactionRepository.ListByUserAsync(userId, query, cancellationToken);
        return transactions.Select(ToDto).ToList();
    }

    public async Task<TransactionDto?> GetAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(userId, transactionId, cancellationToken);
        return transaction is null ? null : ToDto(transaction);
    }

    public async Task<TransactionDto> CreateAsync(UserId userId, CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var tagId = new TagId(request.TagId);
        await EnsureDescriptionIsProvidedForUncategorizedTagAsync(userId, tagId, request.Description, cancellationToken);

        var transaction = Transaction.Create(
            new TransactionId(NewEntityId.Generate()),
            userId,
            Enum.Parse<TransactionType>(request.Type, ignoreCase: true),
            new Money(request.Amount),
            request.Date,
            request.Description,
            tagId,
            Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
            DateTimeOffset.UtcNow);

        await _transactionRepository.SaveAsync(transaction, cancellationToken);
        return ToDto(transaction);
    }

    public async Task<TransactionDto?> UpdateAsync(
        UserId userId,
        TransactionId transactionId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingTransaction = await _transactionRepository.GetByIdAsync(userId, transactionId, cancellationToken);
        if (existingTransaction is null)
        {
            return null;
        }

        var tagId = new TagId(request.TagId);
        await EnsureDescriptionIsProvidedForUncategorizedTagAsync(userId, tagId, request.Description, cancellationToken);

        var updatedTransaction = Transaction.Create(
            transactionId,
            userId,
            Enum.Parse<TransactionType>(request.Type, ignoreCase: true),
            new Money(request.Amount),
            request.Date,
            request.Description,
            tagId,
            Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
            existingTransaction.CreatedAt,
            existingTransaction.RecurringTransactionId);

        await _transactionRepository.SaveAsync(updatedTransaction, cancellationToken);
        return ToDto(updatedTransaction);
    }

    public async Task<bool> DeleteAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default)
    {
        var existingTransaction = await _transactionRepository.GetByIdAsync(userId, transactionId, cancellationToken);
        if (existingTransaction is null)
        {
            return false;
        }

        await _transactionRepository.DeleteAsync(userId, transactionId, cancellationToken);
        await SkipRecurringMonthAsync(userId, existingTransaction, cancellationToken);

        return true;
    }

    public async Task<DeletedTransactionsDto> DeleteInRangeAsync(
        UserId userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("The start date must not be after the end date.", nameof(from));
        }

        var transactionsInRange = await _transactionRepository.ListByUserAsync(
            userId,
            new TransactionQuery(from, to, TagId: null, Type: null, PaymentMethod: null),
            cancellationToken);
        if (transactionsInRange.Count == 0)
        {
            return new DeletedTransactionsDto(0);
        }

        var transactionIds = transactionsInRange.Select(transaction => transaction.Id).ToList();
        await _transactionRepository.DeleteManyAsync(userId, transactionIds, cancellationToken);

        foreach (var deletedTransaction in transactionsInRange)
        {
            await SkipRecurringMonthAsync(userId, deletedTransaction, cancellationToken);
        }

        return new DeletedTransactionsDto(transactionsInRange.Count);
    }

    /// <summary>
    /// Without this, the next bounded-range query (history, dashboard) would see no transaction
    /// for this recurrence/month and lazily re-materialize the exact one just deleted.
    /// </summary>
    private async Task SkipRecurringMonthAsync(UserId userId, Transaction deletedTransaction, CancellationToken cancellationToken)
    {
        if (deletedTransaction.RecurringTransactionId is not { } recurringTransactionId)
        {
            return;
        }

        await _recurringTransactionService.SkipMonthAsync(userId, recurringTransactionId, YearMonth.FromDate(deletedTransaction.Date), cancellationToken);
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

    private static TransactionDto ToDto(Transaction transaction) => new(
        transaction.Id.Value,
        transaction.Type.ToString(),
        transaction.Amount.Amount,
        transaction.Date,
        transaction.Description,
        transaction.TagId.Value,
        transaction.PaymentMethod.ToString(),
        transaction.RecurringTransactionId?.Value,
        transaction.CreatedAt);
}
