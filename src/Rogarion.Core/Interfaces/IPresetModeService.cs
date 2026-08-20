using Rogarion.Core.Models;

namespace Rogarion.Core.Interfaces;

public interface IPresetModeService
{
    Task<IReadOnlyList<PresetModeDefinition>> GetModesAsync();
    Task<PresetModeDefinition> AddModeAsync(string name, string systemPrompt);
    Task UpdateModeAsync(PresetModeDefinition mode);
    Task DeleteModeAsync(Guid id);
}
