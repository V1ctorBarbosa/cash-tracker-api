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
}
