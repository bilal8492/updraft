using Microsoft.UI.Xaml;

namespace Updraft;

public partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // --demo shows sample data and simulates every winget call, so the UI can be explored (or screenshotted)
        // without touching the installed apps. Nothing is ever installed or removed in this mode.
        WingetService.Demo = Environment.GetCommandLineArgs().Skip(1)
            .Any(a => a.Equals("--demo", StringComparison.OrdinalIgnoreCase));

        _window = new MainWindow();
        _window.Activate();
    }
}
