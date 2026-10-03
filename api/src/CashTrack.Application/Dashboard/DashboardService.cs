using CashTrack.Application.Budgets;
using CashTrack.Application.RecurringTransactions;
using CashTrack.Application.Tags;
using CashTrack.Application.Transactions;
using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Dashboard;

/// <summary>
/// Aggregates transactions, tags, and budgets into the dashboard's derived numbers (Section 19 of
/// the implementation document) -- nothing here is persisted, it's all computed per request from
/// the underlying facts (Section 14: persist facts, not visualizations).
/// </summary>
public sealed class DashboardService
{
    /// <summary>Used for a tag id that no longer resolves to a real tag (deleted after being used on a transaction or budget) -- a neutral, already-used-elsewhere shade so a chart never breaks instead of colorless.</summary>
    private const string DeletedTagFallbackColor = "#B9B3A1";

    private readonly ITransactionRepository _transactionRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly RecurringTransactionMaterializationService _recurringTransactionMaterializationService;

    public DashboardService(
        ITransactionRepository transactionRepository,
        ITagRepository tagRepository,
        IBudgetRepository budgetRepository,
        RecurringTransactionMaterializationService recurringTransactionMaterializationService)
    {
        _transactionRepository = transactionRepository;
        _tagRepository = tagRepository;
        _budgetRepository = budgetRepository;
        _recurringTransactionMaterializationService = recurringTransactionMaterializationService;
    }

    public async Task<DashboardDto> GetAsync(UserId userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await _recurringTransactionMaterializationService.MaterializeAsync(userId, from, to, cancellationToken);

        var transactions = await _transactionRepository.ListByUserAsync(
            userId,
            new TransactionQuery(from, to, TagId: null, Type: null, PaymentMethod: null),
            cancellationToken);
        var tagsById = (await _tagRepository.ListByUserAsync(userId, cancellationToken)).ToDictionary(tag => tag.Id);

        var totalIncome = SumByType(transactions, TransactionType.Income);
        var totalExpense = SumByType(transactions, TransactionType.Expense);

        return new DashboardDto(
            totalIncome,
            totalExpense,
            totalIncome - totalExpense,
            BuildMonthlySummary(transactions, from, to),
            BuildSpendByTag(transactions, tagsById),
            BuildSpendByPaymentMethod(transactions),
            await BuildBudgetsAsync(userId, from, to, transactions, tagsById, cancellationToken));
    }

    private static decimal SumByType(IReadOnlyList<Transaction> transactions, TransactionType type) =>
        transactions.Where(transaction => transaction.Type == type).Sum(transaction => transaction.Amount.Amount);

    /// <summary>One entry per calendar month in [from, to], zero-filled so the frontend never has to fill a gap itself.</summary>
    private static IReadOnlyList<MonthlySummaryDto> BuildMonthlySummary(IReadOnlyList<Transaction> transactions, DateOnly from, DateOnly to)
    {
        var transactionsByMonth = transactions.ToLookup(transaction => YearMonth.FromDate(transaction.Date));

        return YearMonth.EnumerateRange(YearMonth.FromDate(from), YearMonth.FromDate(to))
            .Select(month => new MonthlySummaryDto(
                month.ToString(),
                SumByType(transactionsByMonth[month].ToList(), TransactionType.Income),
                SumByType(transactionsByMonth[month].ToList(), TransactionType.Expense)))
            .ToList();
    }

    private static IReadOnlyList<TagAmountDto> BuildSpendByTag(IReadOnlyList<Transaction> transactions, IReadOnlyDictionary<TagId, Tag> tagsById) =>
        transactions
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .GroupBy(transaction => transaction.TagId)
            .Select(group => new TagAmountDto(
                group.Key.Value,
                ResolveTagName(group.Key, tagsById),
                ResolveTagColor(group.Key, tagsById),
                group.Sum(transaction => transaction.Amount.Amount)))
            .ToList();

    private static IReadOnlyList<PaymentMethodAmountDto> BuildSpendByPaymentMethod(IReadOnlyList<Transaction> transactions) =>
        transactions
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .GroupBy(transaction => transaction.PaymentMethod)
            .Select(group => new PaymentMethodAmountDto(group.Key.ToString(), group.Sum(transaction => transaction.Amount.Amount)))
            .ToList();

    /// <summary>Every Budget whose month falls in [from, to], each carrying the actual expense total spent on its tag that month.</summary>
    private async Task<IReadOnlyList<BudgetProgressDto>> BuildBudgetsAsync(
        UserId userId,
        DateOnly from,
        DateOnly to,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<TagId, Tag> tagsById,
        CancellationToken cancellationToken)
    {
        var fromMonth = YearMonth.FromDate(from);
        var toMonth = YearMonth.FromDate(to);

        var budgetsInRange = (await _budgetRepository.ListByUserAsync(userId, month: null, cancellationToken))
            .Where(budget => budget.Month >= fromMonth && budget.Month <= toMonth);

        return budgetsInRange
            .Select(budget => new BudgetProgressDto(
                budget.TagId.Value,
                ResolveTagName(budget.TagId, tagsById),
                ResolveTagColor(budget.TagId, tagsById),
                budget.Month.ToString(),
                budget.LimitAmount.Amount,
                ActualExpenseForTagAndMonth(transactions, budget.TagId, budget.Month)))
            .ToList();
    }

    private static decimal ActualExpenseForTagAndMonth(IReadOnlyList<Transaction> transactions, TagId tagId, YearMonth month) => transactions
        .Where(transaction =>
            transaction.Type == TransactionType.Expense
            && transaction.TagId == tagId
            && YearMonth.FromDate(transaction.Date) == month)
        .Sum(transaction => transaction.Amount.Amount);

    /// <summary>Falls back to the raw id when a tag was deleted after being used on a transaction or budget.</summary>
    private static string ResolveTagName(TagId tagId, IReadOnlyDictionary<TagId, Tag> tagsById) =>
        tagsById.TryGetValue(tagId, out var tag) ? tag.Name : tagId.Value;

    /// <summary>Every chart must color a tag by its own real, user-assigned color -- never by list position -- so the same category always looks the same across every chart.</summary>
    private static string ResolveTagColor(TagId tagId, IReadOnlyDictionary<TagId, Tag> tagsById) =>
        tagsById.TryGetValue(tagId, out var tag) ? tag.Color.Value : DeletedTagFallbackColor;
}
