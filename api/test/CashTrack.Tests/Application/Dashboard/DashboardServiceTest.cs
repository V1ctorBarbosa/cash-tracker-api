using CashTrack.Application.Budgets;
using CashTrack.Application.Dashboard;
using CashTrack.Application.RecurringTransactions;
using CashTrack.Application.Tags;
using CashTrack.Application.Transactions;
using CashTrack.Domain.Budgets;
using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Transaction = CashTrack.Domain.Transactions.Transaction;

namespace CashTrack.Tests.Application.Dashboard;

public sealed class DashboardServiceTest
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly IRecurringTransactionRepository _recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
    private readonly DashboardService _sut;
    private readonly UserId _userId = new("firebase-uid-123");
    private readonly TagId _moradiaTagId = new("tag-moradia");
    private readonly TagId _mercadoTagId = new("tag-mercado");
    private readonly TagId _lazerTagId = new("tag-lazer");

    public DashboardServiceTest()
    {
        // No recurring transactions by default, so materialization is a real no-op --
        // RecurringTransactionMaterializationService is sealed and can't be substituted.
        _recurringTransactionRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([]);
        var materializationService = new RecurringTransactionMaterializationService(_recurringTransactionRepository, _transactionRepository);

        _tagRepository.ListByUserAsync(_userId, Arg.Any<CancellationToken>()).Returns([
            Tag.Create(_moradiaTagId, _userId, "Moradia", new TagColor("#000000")),
            Tag.Create(_mercadoTagId, _userId, "Mercado", new TagColor("#000000")),
            Tag.Create(_lazerTagId, _userId, "Lazer", new TagColor("#000000")),
        ]);
        _budgetRepository.ListByUserAsync(_userId, null, Arg.Any<CancellationToken>()).Returns([]);
        _transactionRepository
            .ListByUserAsync(_userId, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _sut = new DashboardService(_transactionRepository, _tagRepository, _budgetRepository, materializationService);
    }

    [Fact]
    public async Task GetAsync_ComputesTotalIncomeExpenseAndBalance_FromTransactionsInRange()
    {
        // Arrange -- income 5200.00, expenses 1800.00 + 89.90 + 45.00 = 1934.90
        SetTransactions(
            CreateTransaction(TransactionType.Income, 5200.00m, new DateOnly(2026, 9, 1), _moradiaTagId, PaymentMethod.Pix),
            CreateTransaction(TransactionType.Expense, 1800.00m, new DateOnly(2026, 9, 5), _moradiaTagId, PaymentMethod.TransferenciaBancaria),
            CreateTransaction(TransactionType.Expense, 89.90m, new DateOnly(2026, 9, 7), _mercadoTagId, PaymentMethod.CartaoDebito),
            CreateTransaction(TransactionType.Expense, 45.00m, new DateOnly(2026, 9, 10), _lazerTagId, PaymentMethod.Pix));

        // Act
        var dashboard = await _sut.GetAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        dashboard.TotalIncome.Should().Be(5200.00m);
        dashboard.TotalExpense.Should().Be(1934.90m);
        dashboard.Balance.Should().Be(3265.10m);
    }

    [Fact]
    public async Task GetAsync_BuildsMonthlySummary_ZeroFillingMonthsWithNoTransactions()
    {
        // Arrange -- only August has transactions; September must still appear, zero-filled
        SetTransactions(CreateTransaction(TransactionType.Income, 5200.00m, new DateOnly(2026, 8, 1), _moradiaTagId, PaymentMethod.Pix));

        // Act
        var dashboard = await _sut.GetAsync(_userId, new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 30));

        // Assert
        dashboard.MonthlySummary.Should().HaveCount(2);
        dashboard.MonthlySummary[0].Should().Be(new MonthlySummaryDto("2026-08", 5200.00m, 0m));
        dashboard.MonthlySummary[1].Should().Be(new MonthlySummaryDto("2026-09", 0m, 0m));
    }

    [Fact]
    public async Task GetAsync_BuildsSpendByTag_ExpensesOnly_GroupedByTag()
    {
        // Arrange
        SetTransactions(
            CreateTransaction(TransactionType.Income, 5200.00m, new DateOnly(2026, 9, 1), _moradiaTagId, PaymentMethod.Pix),
            CreateTransaction(TransactionType.Expense, 1800.00m, new DateOnly(2026, 9, 5), _moradiaTagId, PaymentMethod.TransferenciaBancaria),
            CreateTransaction(TransactionType.Expense, 89.90m, new DateOnly(2026, 9, 7), _mercadoTagId, PaymentMethod.CartaoDebito));

        // Act
        var dashboard = await _sut.GetAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert -- income is excluded even though it's tagged "Moradia" too
        dashboard.SpendByTag.Should().BeEquivalentTo(
        [
            new TagAmountDto("tag-moradia", "Moradia", 1800.00m),
            new TagAmountDto("tag-mercado", "Mercado", 89.90m),
        ]);
    }

    [Fact]
    public async Task GetAsync_ResolvesTagName_ToRawId_WhenTagNoLongerExists()
    {
        // Arrange
        var deletedTagId = new TagId("tag-deleted");
        SetTransactions(CreateTransaction(TransactionType.Expense, 45.00m, new DateOnly(2026, 9, 10), deletedTagId, PaymentMethod.Pix));

        // Act
        var dashboard = await _sut.GetAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        dashboard.SpendByTag.Should().ContainSingle(entry => entry.TagId == "tag-deleted" && entry.TagName == "tag-deleted");
    }

    [Fact]
    public async Task GetAsync_BuildsSpendByPaymentMethod_ExpensesOnly_GroupedByPaymentMethod()
    {
        // Arrange
        SetTransactions(
            CreateTransaction(TransactionType.Expense, 1800.00m, new DateOnly(2026, 9, 5), _moradiaTagId, PaymentMethod.TransferenciaBancaria),
            CreateTransaction(TransactionType.Expense, 45.00m, new DateOnly(2026, 9, 10), _lazerTagId, PaymentMethod.Pix),
            CreateTransaction(TransactionType.Expense, 300.00m, new DateOnly(2026, 9, 20), _lazerTagId, PaymentMethod.TransferenciaBancaria));

        // Act
        var dashboard = await _sut.GetAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        dashboard.SpendByPaymentMethod.Should().BeEquivalentTo(
        [
            new PaymentMethodAmountDto("TransferenciaBancaria", 2100.00m),
            new PaymentMethodAmountDto("Pix", 45.00m),
        ]);
    }

    [Fact]
    public async Task GetAsync_IncludesBudget_WithActualExpense_ForItsTagAndMonth()
    {
        // Arrange
        var budget = Budget.Create(_userId, _lazerTagId, new YearMonth(2026, 9), new Money(500.00m));
        _budgetRepository.ListByUserAsync(_userId, null, Arg.Any<CancellationToken>()).Returns([budget]);
        SetTransactions(
            CreateTransaction(TransactionType.Expense, 45.00m, new DateOnly(2026, 9, 10), _lazerTagId, PaymentMethod.Pix),
            // Different tag -- must not count towards the Lazer budget's actual amount.
            CreateTransaction(TransactionType.Expense, 89.90m, new DateOnly(2026, 9, 7), _mercadoTagId, PaymentMethod.CartaoDebito));

        // Act
        var dashboard = await _sut.GetAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        dashboard.Budgets.Should().ContainSingle();
        dashboard.Budgets[0].Should().Be(new BudgetProgressDto("tag-lazer", "Lazer", "2026-09", 500.00m, 45.00m));
    }

    [Fact]
    public async Task GetAsync_ExcludesBudgets_OutsideTheQueriedRange()
    {
        // Arrange
        var budgetOutsideRange = Budget.Create(_userId, _lazerTagId, new YearMonth(2026, 6), new Money(500.00m));
        _budgetRepository.ListByUserAsync(_userId, null, Arg.Any<CancellationToken>()).Returns([budgetOutsideRange]);

        // Act
        var dashboard = await _sut.GetAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        dashboard.Budgets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_TriggersRecurringTransactionMaterialization_ForTheQueriedRange()
    {
        // Act
        await _sut.GetAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        await _recurringTransactionRepository.Received(1).ListByUserAsync(_userId, Arg.Any<CancellationToken>());
    }

    private void SetTransactions(params Transaction[] transactions) =>
        _transactionRepository.ListByUserAsync(_userId, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>()).Returns(transactions);

    private Transaction CreateTransaction(TransactionType type, decimal amount, DateOnly date, TagId tagId, PaymentMethod paymentMethod) =>
        Transaction.Create(
            new TransactionId(Guid.NewGuid().ToString()),
            _userId,
            type,
            new Money(amount),
            date,
            "Descrição",
            tagId,
            paymentMethod,
            DateTimeOffset.UtcNow);
}
