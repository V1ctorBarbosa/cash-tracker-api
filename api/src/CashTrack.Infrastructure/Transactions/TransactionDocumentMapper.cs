using System.Globalization;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
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
        ParseDate(document.Date),
        document.Description,
        new TagId(document.TagId),
        Enum.Parse<PaymentMethod>(document.PaymentMethod),
        document.CreatedAt.ToDateTimeOffset(),
        document.RecurringTransactionId is null ? null : new RecurringTransactionId(document.RecurringTransactionId));

    /// <summary>
    /// Keyed by the month of the transaction's own date, so an occurrence the user has since moved
    /// to another day of the same month still counts as materialized. Null for a transaction not
    /// generated from a recurring definition.
    /// </summary>
    public static RecurringOccurrence? ToRecurringOccurrence(TransactionDocument document)
    {
        if (document.RecurringTransactionId is null)
        {
            return null;
        }

        return new RecurringOccurrence(new RecurringTransactionId(document.RecurringTransactionId), YearMonth.FromDate(ParseDate(document.Date)));
    }

    public static DateOnly ParseDate(string storedDate) => DateOnly.ParseExact(storedDate, DateFormat, CultureInfo.InvariantCulture);

    public static TransactionDocument ToDocument(Transaction transaction) => new()
    {
        UserId = transaction.UserId.Value,
        Type = transaction.Type.ToString(),
        Amount = transaction.Amount.Amount,
        Date = transaction.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
        Description = transaction.Description,
        TagId = transaction.TagId.Value,
        PaymentMethod = transaction.PaymentMethod.ToString(),
        RecurringTransactionId = transaction.RecurringTransactionId?.Value,
        CreatedAt = Timestamp.FromDateTimeOffset(transaction.CreatedAt),
    };
}
