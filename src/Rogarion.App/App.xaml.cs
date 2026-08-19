using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Rogarion.App.ViewModels;
using Rogarion.Core.Interfaces;
using Rogarion.Services.Ollama;
using Rogarion.Services.Persistence;

namespace Rogarion.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = BuildServiceProvider();

        _window = new MainWindow();
        _window.Closed += (_, _) => (Services as IDisposable)?.Dispose();
        _window.Activate();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddHttpClient<IOllamaService, OllamaService>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:11434");
        });

        services.AddSingleton<IChatHistoryService, ChatHistoryService>();

        services.AddTransient<MainViewModel>();

        return services.BuildServiceProvider();
    }
}
