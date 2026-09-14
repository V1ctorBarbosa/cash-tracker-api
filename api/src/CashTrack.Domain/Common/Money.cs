namespace CashTrack.Domain.Common;

/// <summary>
/// A monetary amount, always non-negative. Currency is fixed to BRL for the MVP (Section 10 of
/// the implementation document) -- there is no currency field to keep in sync, so this type
/// carries only the amount.
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
        }

        Amount = amount;
    }
}
