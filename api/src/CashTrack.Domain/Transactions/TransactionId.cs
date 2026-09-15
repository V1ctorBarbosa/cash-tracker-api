using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;

namespace CashTrack.Domain.Transactions;

public readonly record struct TransactionId
{
    public string Value { get; }

    public TransactionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Transaction id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Deterministic from (recurringTransactionId, month) -- materialization (Section 12) can run
    /// concurrently for the same recurring definition and month (e.g. the dashboard firing two
    /// overlapping-range queries at once), and a check-then-create using a random id is a race:
    /// two concurrent calls can both see "not materialized yet" and each create their own
    /// transaction, double-counting the amount. Deriving the id this way makes materializing the
    /// same (recurring, month) pair an idempotent upsert instead.
    /// </summary>
    public static TransactionId ForRecurring(RecurringTransactionId recurringTransactionId, YearMonth month) =>
        new($"recurring_{recurringTransactionId.Value}_{month}");
}
