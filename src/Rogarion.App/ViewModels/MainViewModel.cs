using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Rogarion.Core.Interfaces;
using Rogarion.Core.Models;

namespace Rogarion.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IOllamaService _ollamaService;
    private readonly DispatcherQueue _dispatcherQueue;
    private CancellationTokenSource? _streamCts;

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
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
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

        var historyForRequest = Messages.ToList();
        var assistantMessage = new ChatMessage { Role = ChatRole.Assistant, Content = string.Empty };
        Messages.Add(assistantMessage);

        _streamCts = new CancellationTokenSource();
        IsSending = true;
        try
        {
            await foreach (var delta in _ollamaService.StreamChatAsync(SelectedModel, historyForRequest, _streamCts.Token))
            {
                _dispatcherQueue.TryEnqueue(() => assistantMessage.Content += delta);
            }
        }
        catch (OperationCanceledException)
        {
            // User stopped the stream or the app is closing; partial content is kept as-is.
        }
        catch (HttpRequestException)
        {
            Messages.Remove(assistantMessage);
            ErrorMessage = "Couldn't reach Ollama. Check that it's still running and try again.";
        }
        finally
        {
            IsSending = false;
            _streamCts?.Dispose();
            _streamCts = null;
        }
    }

    [RelayCommand]
    private void StopStreaming()
    {
        _streamCts?.Cancel();
    }
}
