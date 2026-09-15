using CashTrack.Application.Transactions;
using CashTrack.Domain.Common;
using CashTrack.Domain.Users;
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
    private readonly IRecurringTransactionRepository _recurringTransactionRepository;
    private readonly ITransactionRepository _transactionRepository;

    public RecurringTransactionMaterializationService(
        IRecurringTransactionRepository recurringTransactionRepository,
        ITransactionRepository transactionRepository)
    {
        _recurringTransactionRepository = recurringTransactionRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task MaterializeAsync(UserId userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var recurringTransactions = await _recurringTransactionRepository.ListByUserAsync(userId, cancellationToken);
        if (recurringTransactions.Count == 0)
        {
            return;
        }

        foreach (var month in YearMonth.EnumerateRange(YearMonth.FromDate(from), YearMonth.FromDate(to)))
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
