using CashTrack.Application.Transactions;
using CashTrack.Domain.Common;
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

        var requestedToMonth = YearMonth.FromDate(to);
        var latestMaterializableMonth = YearMonth.FromDate(DateOnly.FromDateTime(DateTime.UtcNow)).AddMonths(MaxMonthsAheadToMaterialize);

        var cappedToMonth = requestedToMonth;
        if (cappedToMonth > latestMaterializableMonth)
        {
            cappedToMonth = latestMaterializableMonth;
            _logger.LogWarning(
                "Materialization for user {UserId} requested through {RequestedToMonth} but was capped at {CappedToMonth} -- " +
                "a caller is asking for recurring transactions too far into the future.",
                userId.Value,
                requestedToMonth,
                cappedToMonth);
        }

        foreach (var month in YearMonth.EnumerateRange(YearMonth.FromDate(from), cappedToMonth))
        {
            foreach (var recurringTransaction in recurringTransactions)
            {
                if (!recurringTransaction.AppliesToMonth(month))
                {
                    continue;
                }

                var alreadyExists = await _transactionRepository.ExistsForRecurringTransactionAsync(
                    userId,
                    recurringTransaction.Id,
                    month,
                    cancellationToken);
                if (alreadyExists)
                {
                    continue;
                }

                var transaction = Transaction.Create(
                    TransactionId.ForRecurring(recurringTransaction.Id, month),
                    userId,
                    recurringTransaction.Type,
                    recurringTransaction.Amount,
                    recurringTransaction.GetDateForMonth(month),
                    recurringTransaction.Description,
                    recurringTransaction.TagId,
                    recurringTransaction.PaymentMethod,
                    DateTimeOffset.UtcNow,
                    recurringTransaction.Id);

                await _transactionRepository.SaveAsync(transaction, cancellationToken);
            }
        }
    }
}
