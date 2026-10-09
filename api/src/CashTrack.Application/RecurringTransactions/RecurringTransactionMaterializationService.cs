using CashTrack.Application.Transactions;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Users;
using Microsoft.Extensions.Logging;
using Transaction = CashTrack.Domain.Transactions.Transaction;
using TransactionId = CashTrack.Domain.Transactions.TransactionId;

namespace CashTrack.Application.RecurringTransactions;

/// <summary>
/// Materializes active recurring transactions into real Transaction records for every month in
/// a queried range that doesn't have one yet (Section 12 of the implementation document). No
/// scheduler exists -- this runs lazily, called by whatever query is about to read transactions
/// for a bounded date range.
/// </summary>
public sealed class RecurringTransactionMaterializationService
{
    /// <summary>
    /// Never materialize more than this many months into the future, regardless of the requested
    /// range -- a caller passing an unbounded-looking "all time" range (e.g. a far-future upper
    /// bound used just to mean "everything so far") must not turn into years of pre-generated,
    /// not-yet-happened transactions for every active recurrence.
    /// </summary>
    private const int MaxMonthsAheadToMaterialize = 12;

    private readonly IRecurringTransactionRepository _recurringTransactionRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<RecurringTransactionMaterializationService> _logger;

    public RecurringTransactionMaterializationService(
        IRecurringTransactionRepository recurringTransactionRepository,
        ITransactionRepository transactionRepository,
        ILogger<RecurringTransactionMaterializationService> logger)
    {
        _recurringTransactionRepository = recurringTransactionRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task MaterializeAsync(UserId userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var recurringTransactions = await _recurringTransactionRepository.ListByUserAsync(userId, cancellationToken);
        if (recurringTransactions.Count == 0)
        {
            return;
        }

        var fromMonth = YearMonth.FromDate(from);
        var toMonth = CapToLatestMaterializableMonth(userId, YearMonth.FromDate(to));

        var dueOccurrences = ListDueOccurrences(recurringTransactions, fromMonth, toMonth);
        if (dueOccurrences.Count == 0)
        {
            return;
        }

        var dueRecurringTransactionIds = dueOccurrences.Select(dueOccurrence => dueOccurrence.RecurringTransaction.Id).Distinct().ToList();
        var materializedOccurrences = await _transactionRepository.ListMaterializedOccurrencesAsync(
            userId,
            dueRecurringTransactionIds,
            fromMonth,
            toMonth,
            cancellationToken);

        var missingTransactions = dueOccurrences
            .Where(dueOccurrence => !materializedOccurrences.Contains(new RecurringOccurrence(dueOccurrence.RecurringTransaction.Id, dueOccurrence.Month)))
            .Select(dueOccurrence => BuildTransaction(userId, dueOccurrence))
            .ToList();
        if (missingTransactions.Count == 0)
        {
            return;
        }

        await _transactionRepository.SaveManyAsync(missingTransactions, cancellationToken);
    }

    private YearMonth CapToLatestMaterializableMonth(UserId userId, YearMonth requestedToMonth)
    {
        var latestMaterializableMonth = YearMonth.FromDate(DateOnly.FromDateTime(DateTime.UtcNow)).AddMonths(MaxMonthsAheadToMaterialize);
        if (requestedToMonth <= latestMaterializableMonth)
        {
            return requestedToMonth;
        }

        _logger.LogWarning(
            "Materialization for user {UserId} requested through {RequestedToMonth} but was capped at {CappedToMonth} -- " +
            "a caller is asking for recurring transactions too far into the future.",
            userId.Value,
            requestedToMonth,
            latestMaterializableMonth);

        return latestMaterializableMonth;
    }

    private static IReadOnlyList<DueOccurrence> ListDueOccurrences(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        YearMonth fromMonth,
        YearMonth toMonth) =>
        recurringTransactions
            .SelectMany(recurringTransaction => recurringTransaction
                .MonthsDueBetween(fromMonth, toMonth)
                .Select(month => new DueOccurrence(recurringTransaction, month)))
            .ToList();

    private static Transaction BuildTransaction(UserId userId, DueOccurrence dueOccurrence)
    {
        var recurringTransaction = dueOccurrence.RecurringTransaction;

        return Transaction.Create(
            TransactionId.ForRecurring(recurringTransaction.Id, dueOccurrence.Month),
            userId,
            recurringTransaction.Type,
            recurringTransaction.Amount,
            recurringTransaction.GetDateForMonth(dueOccurrence.Month),
            recurringTransaction.Description,
            recurringTransaction.TagId,
            recurringTransaction.PaymentMethod,
            DateTimeOffset.UtcNow,
            recurringTransaction.Id);
    }

    private sealed record DueOccurrence(RecurringTransaction RecurringTransaction, YearMonth Month);
}
