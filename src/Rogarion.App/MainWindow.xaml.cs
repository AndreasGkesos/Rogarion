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
    }
}
