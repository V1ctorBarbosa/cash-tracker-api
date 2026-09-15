using CashTrack.Infrastructure.Common;
using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.RecurringTransactions;

[FirestoreData]
internal sealed class RecurringTransactionDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Type { get; set; } = string.Empty;

    [FirestoreProperty(ConverterType = typeof(DecimalConverter))]
    public decimal Amount { get; set; }

    [FirestoreProperty]
    public string? Description { get; set; }

    [FirestoreProperty]
    public string TagId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string PaymentMethod { get; set; } = string.Empty;

    [FirestoreProperty]
    public int DayOfMonth { get; set; }

    /// <summary>Stored as "yyyy-MM-dd" -- Google.Cloud.Firestore has no native DateOnly support.</summary>
    [FirestoreProperty]
    public string StartDate { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? EndDate { get; set; }

    [FirestoreProperty]
    public bool Active { get; set; }

    /// <summary>Skipped months, each stored as "yyyy-MM".</summary>
    [FirestoreProperty]
    public List<string> SkippedMonths { get; set; } = [];
}
