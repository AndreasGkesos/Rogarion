namespace Rogarion.Core.Interfaces;

public interface IOllamaService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetInstalledModelsAsync(CancellationToken cancellationToken = default);
}
