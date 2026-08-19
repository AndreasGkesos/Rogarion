using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Rogarion.App.ViewModels;

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

        ViewModel.PropertyChanged += (_, _) => UpdateVisibleState();
        ViewModel.AvailableModels.CollectionChanged += (_, _) => UpdateModelPicker();
        ViewModel.Messages.CollectionChanged += (_, _) => UpdateVisibleState();

        MessagesItemsControl.ItemsSource = ViewModel.Messages;

        _ = ViewModel.InitializeAsync();
        UpdateVisibleState();
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

        SendButton.IsEnabled = !ViewModel.IsSending && !string.IsNullOrWhiteSpace(MessageInputBox.Text);
        SendButton.Visibility = ViewModel.IsSending ? Visibility.Collapsed : Visibility.Visible;
        StopButton.Visibility = ViewModel.IsSending ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MessageInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SendButton.IsEnabled = !ViewModel.IsSending && !string.IsNullOrWhiteSpace(MessageInputBox.Text);
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

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        Send();
    }

    private void MessageInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && !IsShiftPressed())
        {
            e.Handled = true;
            Send();
        }
    }

    private static bool IsShiftPressed()
    {
        var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        return state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    private void Send()
    {
        if (string.IsNullOrWhiteSpace(MessageInputBox.Text) || ViewModel.IsSending)
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
}
