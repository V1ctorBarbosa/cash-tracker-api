using CashTrack.Domain.Tags;
using CashTrack.Domain.Users;

namespace CashTrack.Application.Tags;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<Tag?> GetByIdAsync(UserId userId, TagId tagId, CancellationToken cancellationToken = default);

    Task SaveAsync(Tag tag, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, TagId tagId, CancellationToken cancellationToken = default);
}
