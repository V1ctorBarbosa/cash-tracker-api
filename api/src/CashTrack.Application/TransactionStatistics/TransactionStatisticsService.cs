using CashTrack.Application.Tags;
using CashTrack.Application.Transactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;

namespace CashTrack.Application.TransactionStatistics;

/// <summary>
/// All-time figures about a user's transactions, answered with count aggregations instead of
/// loading every transaction -- the cost stays flat no matter how long the history grows. Like
/// any unbounded read, it does not trigger recurring-transaction materialization.
/// </summary>
public sealed class TransactionStatisticsService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITagRepository _tagRepository;

    public TransactionStatisticsService(ITransactionRepository transactionRepository, ITagRepository tagRepository)
    {
        _transactionRepository = transactionRepository;
        _tagRepository = tagRepository;
    }

    public async Task<TransactionStatisticsDto> GetAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var transactionCountTask = _transactionRepository.CountByUserAsync(userId, tagId: null, cancellationToken);
        var firstTransactionDateTask = _transactionRepository.FindEarliestDateAsync(userId, cancellationToken);
        var tagUsagesTask = ListTagUsagesAsync(userId, cancellationToken);

        await Task.WhenAll(transactionCountTask, firstTransactionDateTask, tagUsagesTask);

        return new TransactionStatisticsDto(await transactionCountTask, await firstTransactionDateTask, await tagUsagesTask);
    }

    /// <summary>One entry per existing tag, including tags never used; transactions whose tag was since deleted have no entry.</summary>
    private async Task<IReadOnlyList<TagUsageDto>> ListTagUsagesAsync(UserId userId, CancellationToken cancellationToken)
    {
        var tags = await _tagRepository.ListByUserAsync(userId, cancellationToken);

        return await Task.WhenAll(tags.Select(tag => CountTagUsageAsync(userId, tag.Id, cancellationToken)));
    }

    private async Task<TagUsageDto> CountTagUsageAsync(UserId userId, TagId tagId, CancellationToken cancellationToken)
    {
        var transactionCount = await _transactionRepository.CountByUserAsync(userId, tagId, cancellationToken);

        return new TagUsageDto(tagId.Value, transactionCount);
    }
}
