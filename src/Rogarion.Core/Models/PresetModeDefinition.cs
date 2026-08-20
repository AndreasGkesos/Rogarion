using CommunityToolkit.Mvvm.ComponentModel;

namespace Rogarion.Core.Models;

public partial class PresetModeDefinition : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsBuiltIn { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _systemPrompt = string.Empty;

    public override string ToString() => Name;
}
