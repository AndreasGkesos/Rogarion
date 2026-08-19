using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rogarion.Core.Interfaces;
using Rogarion.Core.Models;

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

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<string> AvailableModels { get; } = [];

    public ObservableCollection<ChatMessage> Messages { get; } = [];

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

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        var text = DraftMessage.Trim();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(SelectedModel) || IsSending)
        {
            return;
        }

        ErrorMessage = null;

        var userMessage = new ChatMessage { Role = ChatRole.User, Content = text };
        Messages.Add(userMessage);
        DraftMessage = string.Empty;

        IsSending = true;
        try
        {
            var reply = await _ollamaService.SendChatAsync(SelectedModel, [.. Messages]);
            Messages.Add(new ChatMessage { Role = ChatRole.Assistant, Content = reply });
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Couldn't reach Ollama. Check that it's still running and try again.";
        }
        finally
        {
            IsSending = false;
        }
    }
}
