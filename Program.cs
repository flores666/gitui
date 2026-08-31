using Avalonia;

namespace GitUi;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildApp().StartWithClassicDesktopLifetime(args);

    private static AppBuilder BuildApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}
