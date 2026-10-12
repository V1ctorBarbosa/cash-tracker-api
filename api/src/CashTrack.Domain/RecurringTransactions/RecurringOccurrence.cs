using CashTrack.Domain.Common;

namespace CashTrack.Domain.RecurringTransactions;

/// <summary>
/// One month's instance of a recurring definition -- the unit materialization (Section 12) works
/// in: each recurring transaction yields at most one real transaction per occurrence.
/// </summary>
public readonly record struct RecurringOccurrence(RecurringTransactionId RecurringTransactionId, YearMonth Month);
