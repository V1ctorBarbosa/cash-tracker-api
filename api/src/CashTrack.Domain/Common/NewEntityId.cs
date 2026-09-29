namespace CashTrack.Domain.Common;

/// <summary>
/// The id value a freshly created entity gets when nothing else determines it (a deterministic
/// composite like <c>BudgetId.For</c>, or an externally supplied id like Firebase's UID).
/// UUIDv7 over v4: its embedded millisecond timestamp orders ids by creation time, a property
/// entities without their own <c>createdAt</c> field otherwise have no way to get.
/// </summary>
public static class NewEntityId
{
    public static string Generate() => Guid.CreateVersion7().ToString();
}
