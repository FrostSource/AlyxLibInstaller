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

        this.UnhandledException += (sender, e) =>
        {
            DebugConsoleError($"Unhandled Exception: {e.Message}");
            DebugConsoleError($"Stack Trace: {e.Exception.StackTrace}");
            e.Handled = true; // Prevent the application from crashing
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                DebugConsoleError($"Unhandled Exception: {ex}");
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

        DebugConsoleMessage($"Settings at: {SettingsManager.Path}");
    }

    /// <summary>
    /// Logs a normal message to the debug console.
    /// </summary>
    /// <param name="message"></param>
    public static void DebugConsoleMessage(string message)
    {
        MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleNormalTextColor"]);
    }
    /// <summary>
    /// Logs a normal message to the debug console.
    /// Uses a verbose message if verbose mode is enabled.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="verboseMessage"></param>
    public static void DebugConsoleMessage(string message, string verboseMessage)
    {
        if (SettingsManager.Settings.VerboseConsole)
            MainWindow.WriteToDebugConsole(verboseMessage, (Windows.UI.Color)Application.Current.Resources["DebugConsoleNormalTextColor"]);
        else
            MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleNormalTextColor"]);
    }
    /// <summary>
    /// Logs a verbose message to the debug console only if verbose mode is enabled.
    /// </summary>
    /// <param name="message"></param>
    public static void DebugConsoleVerbose(string message)
    {
        if (SettingsManager.Settings.VerboseConsole)
            MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleVerboseTextColor"]);
    }
    /// <summary>
    /// Logs a verbose warning to the debug console only if verbose mode is enabled.
    /// </summary>
    /// <param name="message"></param>
    public static void DebugConsoleVerboseWarning(string message)
    {
        if (SettingsManager.Settings.VerboseConsole)
            MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleVerboseWarningTextColor"]);
    }
    /// <summary>
    /// Logs a warning message to the debug console.
    /// </summary>
    /// <param name="message"></param>
    public static void DebugConsoleWarning(string message)
    {
        MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleWarningTextColor"]);
    }
    /// <summary>
    /// Logs a warning message to the debug console.
    /// Uses a verbose message if verbose mode is enabled.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="verboseMessage"></param>
    public static void DebugConsoleWarning(string message, string verboseMessage)
    {
        if (SettingsManager.Settings.VerboseConsole)
            MainWindow.WriteToDebugConsole(verboseMessage, (Windows.UI.Color)Application.Current.Resources["DebugConsoleWarningTextColor"]);
        else
            MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleWarningTextColor"]);
    }
    /// <summary>
    /// Logs an error message to the debug console.
    /// </summary>
    /// <param name="message"></param>
    public static void DebugConsoleError(string message)
    {
        MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleErrorTextColor"]);
    }
    /// <summary>
    /// Logs an error message to the debug console.
    /// Uses a verbose message if verbose mode is enabled.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="verboseMessage"></param>
    public static void DebugConsoleError(string message, string verboseMessage)
    {
        if (SettingsManager.Settings.VerboseConsole)
            MainWindow.WriteToDebugConsole(verboseMessage, (Windows.UI.Color)Application.Current.Resources["DebugConsoleErrorTextColor"]);
        else
            MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleErrorTextColor"]);
    }
    /// <summary>
    /// Logs a blue info message to the debug console.
    /// </summary>
    /// <param name="message"></param>
    public static void DebugConsoleInfo(string message)
    {
        MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleInfoTextColor"]);
    }
    /// <summary>
    /// Logs a blue info message to the debug console.
    /// Uses a verbose message if verbose mode is enabled.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="verboseMessage"></param>
    public static void DebugConsoleInfo(string message, string verboseMessage)
    {
        if (SettingsManager.Settings.VerboseConsole)
            MainWindow.WriteToDebugConsole(verboseMessage, (Windows.UI.Color)Application.Current.Resources["DebugConsoleInfoTextColor"]);
        else
            MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleInfoTextColor"]);
    }
    /// <summary>
    /// Logs a success message to the debug console.
    /// </summary>
    /// <param name="message"></param>
    public static void DebugConsoleSuccess(string message)
    {
        MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleSuccessTextColor"]);
    }
    /// <summary>
    /// Logs a success message to the debug console.
    /// Uses a verbose message if verbose mode is enabled.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="verboseMessage"></param>
    public static void DebugConsoleSuccess(string message, string verboseMessage)
    {
        if (SettingsManager.Settings.VerboseConsole)
            MainWindow.WriteToDebugConsole(verboseMessage, (Windows.UI.Color)Application.Current.Resources["DebugConsoleSuccessTextColor"]);
        else
            MainWindow.WriteToDebugConsole(message, (Windows.UI.Color)Application.Current.Resources["DebugConsoleSuccessTextColor"]);
    }

    public static MainWindow MainWindow = new();
}
