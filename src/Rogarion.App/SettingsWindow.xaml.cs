using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rogarion.App.ViewModels;
using Rogarion.Core.Models;

namespace Rogarion.App;

public sealed partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;

    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        Title = "Rogarion Settings";

        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        }
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ModesItemsControl.ItemsSource = _viewModel.PresetModes;
    }

    private async void AddModeButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NewModeNameBox.Text.Trim();
        var prompt = NewModePromptBox.Text.Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(prompt))
        {
            return;
        }

        await _viewModel.AddPresetModeCommand.ExecuteAsync((name, prompt));
        NewModeNameBox.Text = string.Empty;
        NewModePromptBox.Text = string.Empty;
        AddModeHeaderText.Text = "Add a custom mode";
    }

    private void DuplicateModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PresetModeDefinition mode })
        {
            return;
        }

        AddModeHeaderText.Text = $"Add a custom mode (copy of {mode.Name})";
        NewModeNameBox.Text = $"{mode.Name} Copy";
        NewModePromptBox.Text = mode.SystemPrompt;
        NewModeNameBox.Focus(FocusState.Programmatic);
        NewModeNameBox.SelectAll();
    }

    private void DeleteModeButton_Click(object sender, RoutedEventArgs e)
    {
        SetConfirmPanelVisibility(sender, Visibility.Visible);
    }

    private async void ConfirmDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PresetModeDefinition mode })
        {
            await _viewModel.DeletePresetModeCommand.ExecuteAsync(mode);
        }
    }

    private void CancelDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        SetConfirmPanelVisibility(sender, Visibility.Collapsed);
    }

    private static void SetConfirmPanelVisibility(object sender, Visibility visibility)
    {
        if (sender is not Button { Tag: PresetModeDefinition } button)
        {
            return;
        }

        // FindName searches within the same DataTemplate instance as the clicked button,
        // regardless of exact visual-tree nesting.
        var current = (DependencyObject)button;
        while (current is not null)
        {
            if (current is FrameworkElement fe && fe.FindName("ItemDeleteConfirmPanel") is FrameworkElement panel)
            {
                panel.Visibility = visibility;
                return;
            }

            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
    }
}
