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
    private readonly IChatHistoryService _chatHistoryService;
    private readonly DispatcherQueue _dispatcherQueue;
    private CancellationTokenSource? _streamCts;
    private ChatSession? _currentSession;

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

    [ObservableProperty]
    private ChatSession? _selectedSession;

    public ObservableCollection<string> AvailableModels { get; } = [];

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public ObservableCollection<ChatSession> Sessions { get; } = [];

    public MainViewModel(IOllamaService ollamaService, IChatHistoryService chatHistoryService)
    {
        _ollamaService = ollamaService;
        _chatHistoryService = chatHistoryService;
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

        var sessions = await _chatHistoryService.GetSessionsAsync();
        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }

        IsCheckingOllama = false;
    }

    [RelayCommand]
    private void NewSession()
    {
        _currentSession = null;
        SelectedSession = null;
        Messages.Clear();
        ErrorMessage = null;
    }

    [RelayCommand]
    private void LoadSession(ChatSession session)
    {
        _currentSession = session;
        SelectedSession = session;
        ErrorMessage = null;

        Messages.Clear();
        foreach (var message in session.Messages)
        {
            Messages.Add(message);
        }
    }

    [RelayCommand]
    private async Task RenameSessionAsync((ChatSession Session, string NewTitle) args)
    {
        var newTitle = args.NewTitle.Trim();
        if (string.IsNullOrEmpty(newTitle))
        {
            return;
        }

        args.Session.Title = newTitle;
        await _chatHistoryService.SaveSessionAsync(args.Session);
    }

    [RelayCommand]
    private async Task DeleteSessionAsync(ChatSession session)
    {
        await _chatHistoryService.DeleteSessionAsync(session.Id);
        Sessions.Remove(session);

        if (_currentSession?.Id == session.Id)
        {
            NewSession();
        }
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

        var isFirstMessageInSession = _currentSession is null;

        if (_currentSession is null)
        {
            _currentSession = new ChatSession
            {
                Model = SelectedModel,
                Title = text.Length > 60 ? text[..60] + "…" : text
            };
            Sessions.Insert(0, _currentSession);
            SelectedSession = _currentSession;
        }

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

            await PersistCurrentSessionAsync();
        }

        if (isFirstMessageInSession && !string.IsNullOrEmpty(assistantMessage.Content))
        {
            _ = GenerateSessionTitleAsync(_currentSession, text, assistantMessage.Content);
        }
    }

    private async Task GenerateSessionTitleAsync(ChatSession session, string userMessage, string assistantReply)
    {
        session.IsGeneratingTitle = true;
        try
        {
            var prompt = new ChatMessage
            {
                Role = ChatRole.User,
                Content = $"""
                    Summarize the following exchange as a short chat title (max 6 words, no quotes, no punctuation at the end).

                    User: {userMessage}
                    Assistant: {assistantReply}
                    """
            };

            var titleBuilder = new System.Text.StringBuilder();
            await foreach (var delta in _ollamaService.StreamChatAsync(session.Model, [prompt]))
            {
                titleBuilder.Append(delta);
            }

            var title = titleBuilder.ToString().Trim().Trim('"');
            if (!string.IsNullOrEmpty(title))
            {
                _dispatcherQueue.TryEnqueue(() => session.Title = title);
                await _chatHistoryService.SaveSessionAsync(session);
            }
        }
        catch (HttpRequestException)
        {
            // Keep the fallback title (truncated first message) if naming fails.
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => session.IsGeneratingTitle = false);
        }
    }

    [RelayCommand]
    private void StopStreaming()
    {
        _streamCts?.Cancel();
    }

    private async Task PersistCurrentSessionAsync()
    {
        if (_currentSession is null)
        {
            return;
        }

        _currentSession.Messages = Messages.ToList();
        await _chatHistoryService.SaveSessionAsync(_currentSession);
    }
}
