using AlyxLib.Logging;
using AlyxLibInstallerShared;
using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services.Dialog;
using AlyxLibInstallerShared.ViewModels;
using AlyxLibInstallerWPF.Extensions;
using AlyxLibInstallerWPF.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace AlyxLibInstallerWPF;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    public App()
    {
        // Ensure log file is created at app startup and mark session start
        FileLogger.LogSessionStart();

        // WPF global exception handling
        this.DispatcherUnhandledException += (sender, e) =>
        {
            FileLogger.Log(e.Exception);
            if (MainWindow is MainWindow mainWindow)
                mainWindow.DebugConsoleError($"Unhandled Exception: {e.Exception.Message}\nSee log for details: {FileLogger.LogFilePath}");
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                FileLogger.Log(ex);
                if (MainWindow is MainWindow mainWindow)
                    mainWindow.DebugConsoleError($"Unhandled Exception: {ex.Message}\nSee log for details: {FileLogger.LogFilePath}");
            }
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        var main = new MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var services = new ServiceCollection();
        services.AddTransient<AlyxLibInstallerSharedViewModel>();
        services.AddScoped<IDialogService>(sp => new WpfDialogService(main));
        services.AddScoped<ILogger>(sp => new AlyxLibInstallerLogger(main));

        Services = services.BuildServiceProvider();

        base.OnStartup(e);

        var vm = Services.GetRequiredService<AlyxLibInstallerSharedViewModel>();

        vm.OnThemeChanged += async (object? sender, AppTheme theme) =>
        {
            // Wait for menu to close
            await Task.Yield();

            var newTheme = theme.ToThemeMode();
            if (ThemeMode == newTheme) return;
            ThemeMode = newTheme;
        };

        main.DataContext = vm;

        // Check for command-line arguments
        string[] cmdArgs = Environment.GetCommandLineArgs();
        // Expecting: [0]=exe path, [1]=addon path (optional), selected during initialization so it overrides LastAddon
        if (cmdArgs.Length > 1)
            vm.StartupAddonPath = cmdArgs[1];

        main.Show();
        await vm.InitializeAsync();
    }
}
