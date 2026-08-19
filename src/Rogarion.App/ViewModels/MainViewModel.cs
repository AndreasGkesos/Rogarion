using CommunityToolkit.Mvvm.ComponentModel;

namespace Rogarion.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";
}
