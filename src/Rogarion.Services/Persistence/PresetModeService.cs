using LiteDB;
using Rogarion.Core.Interfaces;
using Rogarion.Core.Models;

namespace Rogarion.Services.Persistence;

public class PresetModeService : IPresetModeService
{
    private const string CollectionName = "presetModes";

    private readonly ILiteCollection<PresetModeDefinition> _modes;

    public PresetModeService(LiteDbContext dbContext)
    {
        _modes = dbContext.GetCollection<PresetModeDefinition>(CollectionName);
        SeedBuiltInModesIfMissing();
    }

    private void SeedBuiltInModesIfMissing()
    {
        if (_modes.Count() > 0)
        {
            return;
        }

        _modes.InsertBulk(BuiltInModes());
    }

    private static IEnumerable<PresetModeDefinition> BuiltInModes()
    {
        yield return new PresetModeDefinition
        {
            IsBuiltIn = true,
            Name = "Refactor",
            SystemPrompt = "You are a senior software engineer. When the user provides code, suggest a cleaner, more idiomatic refactor. Keep explanations brief and focus on concrete improvements."
        };
        yield return new PresetModeDefinition
        {
            IsBuiltIn = true,
            Name = "Explain",
            SystemPrompt = "You are a senior software engineer helping someone learn. When the user provides code, explain clearly and concisely what it does and why, suitable for someone building their understanding."
        };
        yield return new PresetModeDefinition
        {
            IsBuiltIn = true,
            Name = "Find Bugs",
            SystemPrompt = "You are a senior software engineer doing a code review. When the user provides code, look for bugs, edge cases, and potential issues. List them clearly with brief explanations."
        };
    }

    public Task<IReadOnlyList<PresetModeDefinition>> GetModesAsync()
    {
        var modes = _modes.Query()
            .OrderByDescending(m => m.IsBuiltIn)
            .ToList();

        return Task.FromResult<IReadOnlyList<PresetModeDefinition>>(modes);
    }

    public Task<PresetModeDefinition> AddModeAsync(string name, string systemPrompt)
    {
        var mode = new PresetModeDefinition
        {
            Name = name,
            SystemPrompt = systemPrompt,
            IsBuiltIn = false
        };
        _modes.Insert(mode);
        return Task.FromResult(mode);
    }

    public Task UpdateModeAsync(PresetModeDefinition mode)
    {
        if (mode.IsBuiltIn)
        {
            throw new InvalidOperationException("Built-in preset modes can't be edited.");
        }

        _modes.Update(mode);
        return Task.CompletedTask;
    }

    public Task DeleteModeAsync(Guid id)
    {
        var mode = _modes.FindById(id);
        if (mode is { IsBuiltIn: true })
        {
            throw new InvalidOperationException("Built-in preset modes can't be deleted.");
        }

        _modes.Delete(id);
        return Task.CompletedTask;
    }
}
