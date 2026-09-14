using CashTrack.Domain.Common;
using CashTrack.Domain.RecurringTransactions;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.RecurringTransactions;

public sealed class RecurringTransactionService
{
    private const bool ActiveOnCreate = true;

    private readonly IRecurringTransactionRepository _recurringTransactionRepository;

    public RecurringTransactionService(IRecurringTransactionRepository recurringTransactionRepository)
    {
        _recurringTransactionRepository = recurringTransactionRepository;
    }

    public async Task<IReadOnlyList<RecurringTransactionDto>> ListAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var recurringTransactions = await _recurringTransactionRepository.ListByUserAsync(userId, cancellationToken);
        return recurringTransactions.Select(ToDto).ToList();
    }

    public async Task<RecurringTransactionDto> CreateAsync(
        UserId userId,
        CreateRecurringTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var recurringTransaction = RecurringTransaction.Create(
            new RecurringTransactionId(Guid.NewGuid().ToString()),
            userId,
            Enum.Parse<TransactionType>(request.Type, ignoreCase: true),
            new Money(request.Amount),
            request.Description,
            new TagId(request.TagId),
            Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
            request.DayOfMonth,
            request.StartDate,
            request.EndDate,
            ActiveOnCreate);

        await _recurringTransactionRepository.SaveAsync(recurringTransaction, cancellationToken);
        return ToDto(recurringTransaction);
    }

    public async Task<RecurringTransactionDto?> UpdateAsync(
        UserId userId,
        RecurringTransactionId recurringTransactionId,
        UpdateRecurringTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingRecurringTransaction = await _recurringTransactionRepository.GetByIdAsync(userId, recurringTransactionId, cancellationToken);
        if (existingRecurringTransaction is null)
        {
            return null;
        }

        var updatedRecurringTransaction = RecurringTransaction.Create(
            recurringTransactionId,
            userId,
            Enum.Parse<TransactionType>(request.Type, ignoreCase: true),
            new Money(request.Amount),
            request.Description,
            new TagId(request.TagId),
            Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
            request.DayOfMonth,
            request.StartDate,
            request.EndDate,
            request.Active);

        await _recurringTransactionRepository.SaveAsync(updatedRecurringTransaction, cancellationToken);
        return ToDto(updatedRecurringTransaction);
    }

    public async Task<bool> DeleteAsync(UserId userId, RecurringTransactionId recurringTransactionId, CancellationToken cancellationToken = default)
    {
        var existingRecurringTransaction = await _recurringTransactionRepository.GetByIdAsync(userId, recurringTransactionId, cancellationToken);
        if (existingRecurringTransaction is null)
        {
            return false;
        }

        await _recurringTransactionRepository.DeleteAsync(userId, recurringTransactionId, cancellationToken);
        return true;
    }

    private static RecurringTransactionDto ToDto(RecurringTransaction recurringTransaction) => new(
        recurringTransaction.Id.Value,
        recurringTransaction.Type.ToString(),
        recurringTransaction.Amount.Amount,
        recurringTransaction.Description,
        recurringTransaction.TagId.Value,
        recurringTransaction.PaymentMethod.ToString(),
        recurringTransaction.DayOfMonth,
        recurringTransaction.StartDate,
        recurringTransaction.EndDate,
        recurringTransaction.Active);
}
