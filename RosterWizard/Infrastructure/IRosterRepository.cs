using Kronos.RosterWizard.Domain;

namespace Kronos.RosterWizard.Infrastructure;

public interface IRosterRepository
{
    Task<RosterData> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Action<RosterData> update, CancellationToken cancellationToken = default);
    Task ReplaceAsync(RosterData data, CancellationToken cancellationToken = default);
}
