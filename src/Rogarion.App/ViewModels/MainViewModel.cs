using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Rogarion.Core.Interfaces;

namespace Rogarion.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IOllamaService _ollamaService;

    [ObservableProperty]
    private bool _isCheckingOllama = true;

    [ObservableProperty]
    private bool _isOllamaAvailable;

    [ObservableProperty]
    private bool _hasNoModels;

    [ObservableProperty]
    private string? _selectedModel;

    public ObservableCollection<string> AvailableModels { get; } = [];

    public MainViewModel(IOllamaService ollamaService)
    {
        _ollamaService = ollamaService;
    }

    public async Task InitializeAsync()
    {
        IsCheckingOllama = true;

        var isAvailable = await _ollamaService.IsAvailableAsync();
        IsOllamaAvailable = isAvailable;

        if (isAvailable)
        {
            var models = await _ollamaService.GetInstalledModelsAsync();

            AvailableModels.Clear();
            foreach (var model in models)
            {
                AvailableModels.Add(model);
            }

            HasNoModels = AvailableModels.Count == 0;
            SelectedModel = AvailableModels.FirstOrDefault();
        }

        IsCheckingOllama = false;
    }
}
