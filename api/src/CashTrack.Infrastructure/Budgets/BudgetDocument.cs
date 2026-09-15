using CashTrack.Infrastructure.Common;
using Google.Cloud.Firestore;

namespace CashTrack.Infrastructure.Budgets;

[FirestoreData]
internal sealed class BudgetDocument
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string TagId { get; set; } = string.Empty;

    /// <summary>Stored as "yyyy-MM".</summary>
    [FirestoreProperty]
    public string Month { get; set; } = string.Empty;

    [FirestoreProperty(ConverterType = typeof(DecimalConverter))]
    public decimal LimitAmount { get; set; }
}
