using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
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

        ViewModel.PropertyChanged += (_, _) => UpdateVisibleState();
        ViewModel.AvailableModels.CollectionChanged += (_, _) => UpdateModelPicker();

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
        EmptyStatePanel.Visibility = ready ? Visibility.Visible : Visibility.Collapsed;
        InputBar.Visibility = ready ? Visibility.Visible : Visibility.Collapsed;
        TopBar.Visibility = ready ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateModelPicker()
    {
        ModelPickerComboBox.ItemsSource = ViewModel.AvailableModels;
        if (ModelPickerComboBox.SelectedItem is null && ViewModel.AvailableModels.Count > 0)
        {
            ModelPickerComboBox.SelectedIndex = 0;
        }
    }
}
