using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace FileUpdaterClient;

public partial class MainWindow : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Main();
            desktop.Exit += (_, _) => UpdateHandler.Cancel();
        }

        base.OnFrameworkInitializationCompleted();
    }
}