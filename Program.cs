using Avalonia;

namespace FileUpdaterClient;

internal class Program
{
    // This is required by Avalonia
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<MainWindow>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    // Entry point of the application
    public static void Main(string[] args)
        => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
}