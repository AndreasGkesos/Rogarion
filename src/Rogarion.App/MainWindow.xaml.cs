using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Rogarion.App.Controls;
using Rogarion.App.ViewModels;
using Rogarion.Core.Models;
using DispatcherTimer = Microsoft.UI.Xaml.DispatcherTimer;

namespace Rogarion.App;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        Title = "Rogarion";

        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        }
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SetWindowIcon();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.AvailableModels.CollectionChanged += (_, _) => UpdateModelPicker();
        ViewModel.Messages.CollectionChanged += (_, _) => UpdateVisibleState();

        MessagesItemsControl.ItemsSource = ViewModel.Messages;
        SessionListView.ItemsSource = ViewModel.Sessions;

        ViewModel.PresetModes.CollectionChanged += (_, _) => UpdateModePicker();
        UpdateModePicker();

        FileChipsControl.ItemsSource = ViewModel.PendingFiles;
        ViewModel.PendingFiles.CollectionChanged += (_, _) => UpdateFileChipsVisibility();
        UpdateFileChipsVisibility();

        _ = ViewModel.InitializeAsync();
        UpdateVisibleState();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateVisibleState();

        if (e.PropertyName == nameof(MainViewModel.SelectedSession)
            && !ReferenceEquals(SessionListView.SelectedItem, ViewModel.SelectedSession))
        {
            SessionListView.SelectedItem = ViewModel.SelectedSession;
        }

        if (e.PropertyName is nameof(MainViewModel.ContextUsagePercent) or nameof(MainViewModel.ContextWindowTokens))
        {
            UpdateContextUsageDisplay();
        }
    }

    private void UpdateContextUsageDisplay()
    {
        ContextUsageTextBlock.Text = ViewModel.ContextUsagePercent > 0
            ? $"context: {ViewModel.ContextUsagePercent:0}%"
            : string.Empty;
    }

    private void UpdateVisibleState()
    {
        CheckingPanel.Visibility = ViewModel.IsCheckingOllama ? Visibility.Visible : Visibility.Collapsed;

        var unreachable = !ViewModel.IsCheckingOllama && !ViewModel.IsOllamaAvailable;
        UnreachablePanel.Visibility = unreachable ? Visibility.Visible : Visibility.Collapsed;

        var noModels = !ViewModel.IsCheckingOllama && ViewModel.IsOllamaAvailable && ViewModel.HasNoModels;
        NoModelsPanel.Visibility = noModels ? Visibility.Visible : Visibility.Collapsed;

        var ready = !ViewModel.IsCheckingOllama && ViewModel.IsOllamaAvailable && !ViewModel.HasNoModels;
        var hasMessages = ViewModel.Messages.Count > 0;

        EmptyStatePanel.Visibility = ready && !hasMessages ? Visibility.Visible : Visibility.Collapsed;
        MessagesScrollViewer.Visibility = ready && hasMessages ? Visibility.Visible : Visibility.Collapsed;
        InputBar.Visibility = ready ? Visibility.Visible : Visibility.Collapsed;
        TopBar.Visibility = ready ? Visibility.Visible : Visibility.Collapsed;

        ErrorTextBlock.Text = ViewModel.ErrorMessage ?? string.Empty;
        ErrorTextBlock.Visibility = !string.IsNullOrEmpty(ViewModel.ErrorMessage) ? Visibility.Visible : Visibility.Collapsed;

        SendButton.IsEnabled = CanSend();
        SendButton.Visibility = ViewModel.IsSending ? Visibility.Collapsed : Visibility.Visible;
        StopButton.Visibility = ViewModel.IsSending ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }
    }

    private void MessageInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SendButton.IsEnabled = CanSend();
    }

    private bool CanSend()
    {
        return !ViewModel.IsSending
            && (!string.IsNullOrWhiteSpace(MessageInputBox.Text) || ViewModel.PendingFiles.Count > 0);
    }

    private void UpdateFileChipsVisibility()
    {
        FileChipsControl.Visibility = ViewModel.PendingFiles.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        SendButton.IsEnabled = CanSend();
    }

    private void UpdateModelPicker()
    {
        ModelPickerComboBox.ItemsSource = ViewModel.AvailableModels;
        if (ModelPickerComboBox.SelectedItem is null && ViewModel.AvailableModels.Count > 0)
        {
            ModelPickerComboBox.SelectedIndex = 0;
        }
    }

    private void ModelPickerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedModel = ModelPickerComboBox.SelectedItem as string;
    }

    private void UpdateModePicker()
    {
        var items = new List<object> { "None" };
        items.AddRange(ViewModel.PresetModes);

        var previouslySelectedId = (ModePickerComboBox.SelectedItem as PresetModeDefinition)?.Id;

        ModePickerComboBox.ItemsSource = items;

        ModePickerComboBox.SelectedIndex = previouslySelectedId is { } id
            ? items.FindIndex(i => (i as PresetModeDefinition)?.Id == id) is var index and >= 0 ? index : 0
            : 0;
    }

    private void ModePickerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedMode = ModePickerComboBox.SelectedItem as PresetModeDefinition;
    }

    private SettingsWindow? _settingsWindow;

    private void ModeSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(ViewModel);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Activate();
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        Send();
    }

    private void EnterAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Send();
    }

    private void Send()
    {
        if (!CanSend())
        {
            return;
        }

        ViewModel.DraftMessage = MessageInputBox.Text;
        MessageInputBox.Text = string.Empty;
        _ = ViewModel.SendMessageCommand.ExecuteAsync(null);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StopStreamingCommand.Execute(null);
    }

    private void RemoveFileChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Rogarion.Core.Models.PendingFile file })
        {
            ViewModel.RemovePendingFileCommand.Execute(file);
        }
    }

    private void InputDropTarget_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems)
            ? Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy
            : Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
    }

    private async void InputDropTarget_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        var files = items
            .OfType<Windows.Storage.StorageFile>()
            .Select(f => (f.Name, ReadBytesAsync: (Func<Task<byte[]>>)(async () =>
            {
                using var stream = await f.OpenStreamForReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            })))
            .ToList();

        if (files.Count == 0)
        {
            return;
        }

        var rejections = await ViewModel.AddFilesAsync(files);
        if (rejections.Count > 0)
        {
            ShowTransientError(string.Join("; ", rejections));
        }
    }

    private DispatcherTimer? _transientErrorTimer;

    private void ShowTransientError(string message)
    {
        ViewModel.ErrorMessage = message;

        _transientErrorTimer?.Stop();
        _transientErrorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _transientErrorTimer.Tick += (_, _) =>
        {
            _transientErrorTimer?.Stop();
            if (ViewModel.ErrorMessage == message)
            {
                ViewModel.ErrorMessage = null;
            }
        };
        _transientErrorTimer.Start();
    }

    private void NewChatButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NewSessionCommand.Execute(null);
        MessageInputBox.Focus(FocusState.Programmatic);
    }

    private void SessionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SessionListView.SelectedItem is ChatSession session)
        {
            ViewModel.LoadSessionCommand.Execute(session);
        }
    }

    private async void RenameSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ChatSession session })
        {
            return;
        }

        var currentTitle = session.Title ?? string.Empty;
        var textBox = new TextBox
        {
            Text = currentTitle,
            SelectionStart = currentTitle.Length
        };

        var dialog = new ContentDialog
        {
            Title = "Rename conversation",
            Content = textBox,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RenameSessionCommand.ExecuteAsync((session, textBox.Text));
        }
    }

    private async void DeleteSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ChatSession session })
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Delete this conversation?",
            Content = "This can't be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteSessionCommand.ExecuteAsync(session);
        }
    }
}
