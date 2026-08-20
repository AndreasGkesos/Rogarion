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
    private readonly IPresetModeService _presetModeService;
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

    [ObservableProperty]
    private PresetModeDefinition? _selectedMode;

    public ObservableCollection<string> AvailableModels { get; } = [];

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public ObservableCollection<ChatSession> Sessions { get; } = [];

    public ObservableCollection<PresetModeDefinition> PresetModes { get; } = [];

    public MainViewModel(IOllamaService ollamaService, IChatHistoryService chatHistoryService, IPresetModeService presetModeService)
    {
        _ollamaService = ollamaService;
        _chatHistoryService = chatHistoryService;
        _presetModeService = presetModeService;
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

        await ReloadPresetModesAsync();

        IsCheckingOllama = false;
    }

    public async Task ReloadPresetModesAsync()
    {
        var previouslySelectedId = SelectedMode?.Id;

        var modes = await _presetModeService.GetModesAsync();
        PresetModes.Clear();
        foreach (var mode in modes)
        {
            PresetModes.Add(mode);
        }

        SelectedMode = previouslySelectedId is { } id
            ? PresetModes.FirstOrDefault(m => m.Id == id)
            : null;
    }

    [RelayCommand]
    private async Task AddPresetModeAsync((string Name, string SystemPrompt) args)
    {
        var name = args.Name.Trim();
        var prompt = args.SystemPrompt.Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(prompt))
        {
            return;
        }

        await _presetModeService.AddModeAsync(name, prompt);
        await ReloadPresetModesAsync();
    }

    [RelayCommand]
    private async Task UpdatePresetModeAsync(PresetModeDefinition mode)
    {
        await _presetModeService.UpdateModeAsync(mode);
        await ReloadPresetModesAsync();
    }

    [RelayCommand]
    private async Task DeletePresetModeAsync(PresetModeDefinition mode)
    {
        await _presetModeService.DeleteModeAsync(mode.Id);
        await ReloadPresetModesAsync();
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

        var userMessage = new ChatMessage { Role = ChatRole.User, Content = text, ModeName = SelectedMode?.Name };
        Messages.Add(userMessage);
        DraftMessage = string.Empty;

        var historyForRequest = BuildRequestHistory();
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

    private List<ChatMessage> BuildRequestHistory()
    {
        var systemPrompt = SelectedMode?.SystemPrompt;
        if (string.IsNullOrEmpty(systemPrompt))
        {
            return Messages.ToList();
        }

        // Insert immediately before the latest (current) user message rather than at the
        // very start of history, so the active mode has more weight than earlier turns that
        // may have been sent under a different mode (or no mode at all). The style-override
        // sentence keeps the model from imitating its own prior reply's tone/structure while
        // still letting it use the conversation's actual content as context.
        var scopedPrompt = $"{systemPrompt} For this reply, follow this instruction's style and focus, even if your earlier replies in this conversation used a different approach. You may still reference the conversation's content, just not its previous response style.";

        var history = new List<ChatMessage>(Messages);
        var insertIndex = history.Count > 0 ? history.Count - 1 : 0;
        history.Insert(insertIndex, new ChatMessage { Role = ChatRole.System, Content = scopedPrompt });
        return history;
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
