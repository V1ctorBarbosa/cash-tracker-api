using CashTrack.Domain.Common;
using CashTrack.Domain.Tags;
using CashTrack.Domain.Transactions;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Transactions;

public sealed class TransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IReadOnlyList<TransactionDto>> ListAsync(
        UserId userId,
        DateOnly? from,
        DateOnly? to,
        string? tagId,
        string? type,
        string? paymentMethod,
        CancellationToken cancellationToken = default)
    {
        var query = new TransactionQuery(
            from,
            to,
            tagId is null ? null : new TagId(tagId),
            type is null ? null : Enum.Parse<TransactionType>(type, ignoreCase: true),
            paymentMethod is null ? null : Enum.Parse<PaymentMethod>(paymentMethod, ignoreCase: true));

        var transactions = await _transactionRepository.ListByUserAsync(userId, query, cancellationToken);
        return transactions.Select(ToDto).ToList();
    }

    public async Task<TransactionDto?> GetAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(userId, transactionId, cancellationToken);
        return transaction is null ? null : ToDto(transaction);
    }

    public async Task<TransactionDto> CreateAsync(UserId userId, CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var transaction = Transaction.Create(
            new TransactionId(Guid.NewGuid().ToString()),
            userId,
            Enum.Parse<TransactionType>(request.Type, ignoreCase: true),
            new Money(request.Amount),
            request.Date,
            request.Description,
            new TagId(request.TagId),
            Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
            DateTimeOffset.UtcNow);

        await _transactionRepository.SaveAsync(transaction, cancellationToken);
        return ToDto(transaction);
    }

    public async Task<TransactionDto?> UpdateAsync(
        UserId userId,
        TransactionId transactionId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingTransaction = await _transactionRepository.GetByIdAsync(userId, transactionId, cancellationToken);
        if (existingTransaction is null)
        {
            return null;
        }

        var updatedTransaction = Transaction.Create(
            transactionId,
            userId,
            Enum.Parse<TransactionType>(request.Type, ignoreCase: true),
            new Money(request.Amount),
            request.Date,
            request.Description,
            new TagId(request.TagId),
            Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true),
            existingTransaction.CreatedAt);

        await _transactionRepository.SaveAsync(updatedTransaction, cancellationToken);
        return ToDto(updatedTransaction);
    }

    public async Task<bool> DeleteAsync(UserId userId, TransactionId transactionId, CancellationToken cancellationToken = default)
    {
        var existingTransaction = await _transactionRepository.GetByIdAsync(userId, transactionId, cancellationToken);
        if (existingTransaction is null)
        {
            return false;
        }

        await _transactionRepository.DeleteAsync(userId, transactionId, cancellationToken);
        return true;
    }

    private static TransactionDto ToDto(Transaction transaction) => new(
        transaction.Id.Value,
        transaction.Type.ToString(),
        transaction.Amount.Amount,
        transaction.Date,
        transaction.Description,
        transaction.TagId.Value,
        transaction.PaymentMethod.ToString(),
        transaction.CreatedAt);
}
