namespace CashTrack.Domain.Common;

/// <summary>
/// Fixed, closed set of ways a transaction was paid -- not a user-managed entity
/// (Section 8 of the implementation document).
/// </summary>
public enum PaymentMethod
{
    Pix,
    Dinheiro,
    CartaoDebito,
    CartaoCredito,
    TransferenciaBancaria,
    Boleto,
    Outro,
}
