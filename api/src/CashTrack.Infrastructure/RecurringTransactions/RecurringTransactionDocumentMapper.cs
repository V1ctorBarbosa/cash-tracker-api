using System.Globalization;
using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Infrastructure.RecurringTransactions;

/// <summary>
/// Pure mapping between the domain's <see cref="RecurringTransaction"/> entity and its Firestore
/// persistence representation. Kept free of any Firestore I/O so it can be unit tested without a
/// live database.
/// </summary>
internal static class RecurringTransactionDocumentMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static RecurringTransaction ToDomain(RecurringTransactionId id, RecurringTransactionDocument document) => RecurringTransaction.Create(
        id,
        new UserId(document.UserId),
        Enum.Parse<TransactionType>(document.Type),
        new Money(document.Amount),
        document.Description,
        new TagId(document.TagId),
        Enum.Parse<PaymentMethod>(document.PaymentMethod),
        document.DayOfMonth,
        DateOnly.ParseExact(document.StartDate, DateFormat, CultureInfo.InvariantCulture),
        document.EndDate is null ? null : DateOnly.ParseExact(document.EndDate, DateFormat, CultureInfo.InvariantCulture),
        document.Active);

    public static RecurringTransactionDocument ToDocument(RecurringTransaction recurringTransaction) => new()
    {
        UserId = recurringTransaction.UserId.Value,
        Type = recurringTransaction.Type.ToString(),
        Amount = recurringTransaction.Amount.Amount,
        Description = recurringTransaction.Description,
        TagId = recurringTransaction.TagId.Value,
        PaymentMethod = recurringTransaction.PaymentMethod.ToString(),
        DayOfMonth = recurringTransaction.DayOfMonth,
        StartDate = recurringTransaction.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture),
        EndDate = recurringTransaction.EndDate?.ToString(DateFormat, CultureInfo.InvariantCulture),
        Active = recurringTransaction.Active,
    };
}
