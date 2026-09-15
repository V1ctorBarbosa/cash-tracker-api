using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.Common;

/// <summary>
/// Firestore has no native decimal type, and its default double representation loses precision
/// for money (e.g. 0.1 + 0.2 != 0.3) -- this stores every decimal as an integer number of cents
/// instead, which round-trips every BRL amount in this domain (always two decimal places)
/// exactly. Applied per-property via <c>[FirestoreProperty(ConverterType = typeof(DecimalConverter))]</c>.
/// </summary>
internal sealed class DecimalConverter : IFirestoreConverter<decimal>
{
    private const int CentsPerUnit = 100;

    public object ToFirestore(decimal value) => (long)Math.Round(value * CentsPerUnit, 0, MidpointRounding.AwayFromZero);

    public decimal FromFirestore(object value) => value switch
    {
        long cents => (decimal)cents / CentsPerUnit,
        double amount => (decimal)amount / CentsPerUnit,
        null => throw new ArgumentNullException(nameof(value)),
        _ => throw new ArgumentException($"Unexpected type for decimal conversion: {value.GetType()}", nameof(value)),
    };
}
