namespace CashTrack.Domain.RecurringTransactions;

public readonly record struct RecurringTransactionId
{
    public string Value { get; }

    public RecurringTransactionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Recurring transaction id cannot be empty.", nameof(value));
        }

        Value = value;
    }
}
