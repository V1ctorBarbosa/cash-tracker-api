using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.Transactions;

[FirestoreData]
internal sealed class TransactionDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Type { get; set; } = string.Empty;

    [FirestoreProperty]
    public decimal Amount { get; set; }

    /// <summary>Stored as "yyyy-MM-dd" -- Google.Cloud.Firestore has no native DateOnly support.</summary>
    [FirestoreProperty]
    public string Date { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? Description { get; set; }

    [FirestoreProperty]
    public string TagId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string PaymentMethod { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? RecurringTransactionId { get; set; }

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }
}
