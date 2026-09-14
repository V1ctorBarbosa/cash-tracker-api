using System.Globalization;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using Google.Cloud.Firestore;
// Google.Cloud.Firestore also defines a Transaction type (its own transaction/batched-write
// primitive) -- alias ours explicitly so every unqualified `Transaction` below means the
// domain entity.
using Transaction = CashTrack.Domain.Transactions.Transaction;

namespace CashTrack.Infrastructure.Transactions;

/// <summary>
/// Pure mapping between the domain's <see cref="Transaction"/> entity and its Firestore
/// persistence representation. Kept free of any Firestore I/O so it can be unit tested
/// without a live database.
/// </summary>
internal static class TransactionDocumentMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static Transaction ToDomain(TransactionId id, TransactionDocument document) => Transaction.Create(
        id,
        new UserId(document.UserId),
        Enum.Parse<TransactionType>(document.Type),
        new Money(document.Amount),
        DateOnly.ParseExact(document.Date, DateFormat, CultureInfo.InvariantCulture),
        document.Description,
        new TagId(document.TagId),
        Enum.Parse<PaymentMethod>(document.PaymentMethod),
        document.CreatedAt.ToDateTimeOffset());

    public static TransactionDocument ToDocument(Transaction transaction) => new()
    {
        UserId = transaction.UserId.Value,
        Type = transaction.Type.ToString(),
        Amount = transaction.Amount.Amount,
        Date = transaction.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
        Description = transaction.Description,
        TagId = transaction.TagId.Value,
        PaymentMethod = transaction.PaymentMethod.ToString(),
        CreatedAt = Timestamp.FromDateTimeOffset(transaction.CreatedAt),
    };
}
