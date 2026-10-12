namespace CashTrack.Application.TransactionStatistics;

/// <summary>Null <paramref name="FirstTransactionDate"/> means the user has no transactions yet.</summary>
public sealed record TransactionStatisticsDto(
    int TransactionCount,
    DateOnly? FirstTransactionDate,
    IReadOnlyList<TagUsageDto> TagUsages);
