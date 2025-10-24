using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlyxLibInstaller;
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // Ensure log file is created at app startup and mark session start
        FileLogger.LogSessionStart();

        this.UnhandledException += (sender, e) =>
        {
            // Log full exception with stack trace to file
            FileLogger.Log(e.Exception);
            DebugConsoleError($"Unhandled Exception: {e.Message}\nSee log for details: {FileLogger.LogFilePath}");
            e.Handled = true; // Prevent the application from crashing
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                // Log full exception with stack trace to file
                FileLogger.Log(ex);
                DebugConsoleError($"Unhandled Exception: {ex.Message}\nSee log for details: {FileLogger.LogFilePath}");
            }
        };
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        //MainWindow = new MainWindow();
        //MainWindow.SetWindowSize(400, 400);
        MainWindow.CenterOnScreen();
        MainWindow.Activate();
        MainWindow.ExtendsContentIntoTitleBar = true;

        DebugConsoleMessage($"Settings file: {SettingsManager.Path}");
        DebugConsoleMessage($"Log file: {FileLogger.LogFilePath}");

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
                MainWindow.RequestedAddonName = addonName;
            }
        }
    }

    private static void Log(string message, string? verboseMessage, string colorResourceKey, bool verboseOnly = false)
    {
        bool verbose = SettingsManager.Settings.VerboseConsole;

        if (verboseOnly && !verbose)
            return;

        string msgToLog = (verbose && !string.IsNullOrEmpty(verboseMessage)) ? verboseMessage : message;
        var color = (Windows.UI.Color)Application.Current.Resources[colorResourceKey];

        MainWindow.WriteToDebugConsole(msgToLog, color);
    }

    // Normal message
    public static void DebugConsoleMessage(string message) => Log(message, null, "DebugConsoleNormalTextColor");
    public static void DebugConsoleMessage(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleNormalTextColor");

    // Verbose-only message
    public static void DebugConsoleVerbose(string message) => Log(message, null, "DebugConsoleVerboseTextColor", verboseOnly: true);
    public static void DebugConsoleVerboseWarning(string message) => Log(message, null, "DebugConsoleVerboseWarningTextColor", verboseOnly: true);
    public static void DebugConsoleVerboseError(string message) => Log(message, null, "DebugConsoleErrorTextColor", verboseOnly: true);

    // Warning, Error, Info, Success
    public static void DebugConsoleWarning(string message) => Log(message, null, "DebugConsoleWarningTextColor");
    public static void DebugConsoleWarning(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleWarningTextColor");

    public static void DebugConsoleError(string message) => Log(message, null, "DebugConsoleErrorTextColor");
    public static void DebugConsoleError(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleErrorTextColor");

    public static void DebugConsoleInfo(string message) => Log(message, null, "DebugConsoleInfoTextColor");
    public static void DebugConsoleInfo(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleInfoTextColor");

    public static void DebugConsoleSuccess(string message) => Log(message, null, "DebugConsoleSuccessTextColor");
    public static void DebugConsoleSuccess(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleSuccessTextColor");

    // Exception
    public static void DebugConsoleException(Exception ex)
    {
        Log($"{ex.GetType().Name} exception occurred! Check log for details {FileLogger.LogFilePath}", null, "DebugConsoleErrorTextColor");
        FileLogger.Log(ex);
    }


    public static MainWindow MainWindow = new();
}
