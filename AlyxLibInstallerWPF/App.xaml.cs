using AlyxLib.Logging;
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
    //public static new MainWindow MainWindow = new();

    public static IServiceProvider Services { get; private set; }

    public App()
    {

        //// Ensure log file is created at app startup and mark session start
        //FileLogger.LogSessionStart();

        //// WPF global exception handling
        //this.DispatcherUnhandledException += (sender, e) =>
        //{
        //    FileLogger.Log(e.Exception);
        //    if (MainWindow is MainWindow mainWindow)
        //        mainWindow.DebugConsoleError($"Unhandled Exception: {e.Exception.Message}\nSee log for details: {FileLogger.LogFilePath}");
        //    e.Handled = true;
        //};

        //AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        //{
        //    if (e.ExceptionObject is Exception ex)
        //    {
        //        FileLogger.Log(ex);
        //        if (MainWindow is MainWindow mainWindow)
        //            mainWindow.DebugConsoleError($"Unhandled Exception: {ex.Message}\nSee log for details: {FileLogger.LogFilePath}");
        //    }
        //};
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var main = new MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var services = new ServiceCollection();
        services.AddTransient<AlyxLibInstallerSharedViewModel>();
        services.AddScoped<IDialogService>(sp => new WpfDialogService(main));
        //services.AddSingleton<IFolderPickerService, WpfFolderPickerService>();
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
        //{
        //    DataContext = vm,
        //    WindowStartupLocation = WindowStartupLocation.CenterScreen
        //};

        ///TODO: Decide if cmd line addon should be parsed into viewmodel on init so it overrides settings LastAddon
        // NEW: Check for command-line arguments
        string[] cmdArgs = Environment.GetCommandLineArgs();
        // Expecting: [0]=exe path, [1]=addon path (optional)
        if (cmdArgs.Length > 1)
        {
            string addonPath = cmdArgs[1];
            // Try to extract addon name from path (assuming folder name is addon name)
            string addonName = System.IO.Path.GetFileName(addonPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrEmpty(addonName))
            {
                vm.SelectAddon(addonName);
            }
        }

        main.Show();
        vm.InitializeAsync();
        vm.IsInitializing = false;


        //MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        //MainWindow.Show();

        //DebugConsoleMessage($"Settings file: {SettingsManager.Path}");
        //DebugConsoleMessage($"Log file: {FileLogger.LogFilePath}");

        //// Check for command-line arguments
        //string[] cmdArgs = Environment.GetCommandLineArgs();
        //// [0]=exe path, [1]=addon path (optional)
        //if (cmdArgs.Length > 1)
        //{
        //    string addonPath = cmdArgs[1];
        //    string addonName = System.IO.Path.GetFileName(addonPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        //    if (!string.IsNullOrEmpty(addonName))
        //    {
        //        MainWindow.RequestedAddonName = addonName;
        //    }
        //}
    }

    //private static void Log(string message, string? verboseMessage, string colorResourceKey, bool verboseOnly = false)
    //{
    //    bool verbose = SettingsManager.Settings.VerboseConsole;
    //    if (verboseOnly && !verbose)
    //        return;

    //    string msgToLog = (verbose && !string.IsNullOrEmpty(verboseMessage)) ? verboseMessage : message;

    //    //// WPF: Use basic color mapping, or default to Black
    //    //Color color = colorResourceKey switch
    //    //{
    //    //    "DebugConsoleNormalTextColor" => Colors.Black,
    //    //    "DebugConsoleVerboseTextColor" => Colors.Gray,
    //    //    "DebugConsoleVerboseWarningTextColor" => Colors.Orange,
    //    //    "DebugConsoleErrorTextColor" => Colors.Red,
    //    //    "DebugConsoleWarningTextColor" => Colors.Orange,
    //    //    "DebugConsoleInfoTextColor" => Colors.Blue,
    //    //    "DebugConsoleSuccessTextColor" => Colors.Green,
    //    //    _ => Colors.Black
    //    //};
    //    var color = (Color)Application.Current.Resources[colorResourceKey];

    //    //MainWindow.WriteToDebugConsole(msgToLog, color);
    //}

    //// Normal message
    //public static void DebugConsoleMessage(string message) => Log(message, null, "DebugConsoleNormalTextColor");
    //public static void DebugConsoleMessage(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleNormalTextColor");

    //// Verbose-only message
    //public static void DebugConsoleVerbose(string message) => Log(message, null, "DebugConsoleVerboseTextColor", verboseOnly: true);
    //public static void DebugConsoleVerboseWarning(string message) => Log(message, null, "DebugConsoleVerboseWarningTextColor", verboseOnly: true);
    //public static void DebugConsoleVerboseError(string message) => Log(message, null, "DebugConsoleErrorTextColor", verboseOnly: true);

    //// Warning, Error, Info, Success
    //public static void DebugConsoleWarning(string message) => Log(message, null, "DebugConsoleWarningTextColor");
    //public static void DebugConsoleWarning(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleWarningTextColor");

    //public static void DebugConsoleError(string message) => Log(message, null, "DebugConsoleErrorTextColor");
    //public static void DebugConsoleError(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleErrorTextColor");

    //public static void DebugConsoleInfo(string message) => Log(message, null, "DebugConsoleInfoTextColor");
    //public static void DebugConsoleInfo(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleInfoTextColor");

    //public static void DebugConsoleSuccess(string message) => Log(message, null, "DebugConsoleSuccessTextColor");
    //public static void DebugConsoleSuccess(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleSuccessTextColor");

    //// Exception
    //public static void DebugConsoleException(Exception ex)
    //{
    //    Log($"{ex.GetType().Name} exception occurred! Check log for details {FileLogger.LogFilePath}", null, "DebugConsoleErrorTextColor");
    //    FileLogger.Log(ex);
    //}
}

