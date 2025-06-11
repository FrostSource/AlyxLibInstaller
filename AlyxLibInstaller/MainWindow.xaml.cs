#nullable enable

using AlyxLibInstaller.AlyxLib;
using LibGit2Sharp;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Semver;
using Source2HelperLibrary;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using WinUIEx.Messaging;
using static AlyxLibInstaller.App;
using Microsoft.Win32;
using System.Reflection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlyxLibInstaller;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WinUIEx.WindowEx
{
    private ElementTheme _currentTheme = ElementTheme.Default;

    private readonly AlyxLibManager AlyxLibInstance = new();

    //public ImmutableArray<string> ElementThemeOptions { get; } = ImmutableArray.Create(Enum.GetNames<ElementTheme>());
    public List<Tuple<string, ElementTheme>> ElementThemeOptions { get; } =
    [.. Enum.GetValues(typeof(ElementTheme))
        .Cast<ElementTheme>()
        .Select(theme => new Tuple<string, ElementTheme>(theme.ToString(), theme))];

    private readonly Settings Settings = SettingsManager.Settings;

    private List<Tuple<string, ScriptEditor>> ScriptEditorOptions { get; } = new()
    {
        new Tuple<string, ScriptEditor>("None", ScriptEditor.None),
        new Tuple<string, ScriptEditor>("VS Code", ScriptEditor.VisualStudioCode),
    };

    private bool userHasPrivileges = true;

    public string? RequestedAddonName { get; set; }

    public MainWindow()
    {
        this.InitializeComponent();

        UpdateMenuBarAddonsList();

        /// Fake addon to test what happens when the addon doesn't exist
        //var _newAddonFlyoutItem = new RadioMenuFlyoutItem();
        //_newAddonFlyoutItem.Text = "FAKE";
        //_newAddonFlyoutItem.Click += MenuBarAddonSelection_Click;
        //_newAddonFlyoutItem.GroupName = "addons";
        //MenuBarAddons.Items.Add(_newAddonFlyoutItem);



        //RootGrid.RequestedTheme = ElementTheme.Light;
        //UnselectAddon();

        var root = this.Content as FrameworkElement;
        if (root != null)
        {
            root.Loaded += (s, e) => MainWindow_Activated_FirstTime();
        }
        else
        {
            WriteToDebugConsole($"Could not find root element in MainWindow. Please report this issue.\nThe log file is located at {FileLogger.LogFilePath}\nThe application will not work correctly and should be closed!", Colors.Goldenrod);
            //Environment.Exit(1);
        }
    }

    private async void MainWindow_Activated_FirstTime()
    {
        MenuBarVerboseLogging.IsChecked = Settings.VerboseConsole;

        // Check for log file accumulation and notify user in the debug console if needed
        if (FileLogger.HasTooManyLogFiles())
        {
            // Compose a warning/info message with a clickable logs folder path
            string logsPath = Path.GetDirectoryName(FileLogger.LogFilePath) ?? "";
            string msg =
                $"There are currently {FileLogger.LogFileCount} log files in your logs folder.\n" +
                $"You may want to clean them up to save disk space.\n" +
                $"Logs folder: {logsPath}";
            // Use yellow for warning/info
            DebugConsoleWarning(msg);
        }

        // Dynamic theme setup
        ElementTheme elementTheme = ElementTheme.Default;
        if (!Enum.TryParse(Settings.Theme, out elementTheme))
        {
            DebugConsoleWarning($"Could not parse theme \"{Settings.Theme}\", defaulting to Default.");
            Settings.Theme = ElementTheme.Default.ToString();
        }

        foreach (var theme in ElementThemeOptions)
        {
            var g = new RadioMenuFlyoutItem();
            g.Text = theme.Item1;
            g.Click += MenuBarThemes_Click;
            MenuBarThemes.Items.Add(g);
            if (theme.Item2 == elementTheme)
            {
                g.IsChecked = true;
                RootGrid.RequestedTheme = elementTheme;
            }
        }

        //this.Activated -= MainWindow_Activated_FirstTime;
        SetAlyxLibPath(Settings.AlyxLibDirectory, silent: true);

        if (!PrivilegeChecker.CanCreateSymlinks())
        {
            //TODO: Show popup to user about symlinks and how to enable them
            //throw new NotImplementedException("This installer requires developer mode or administrator privileges to install.");
            userHasPrivileges = false;
            ShowPrivilegeWarningPopup();
        }
        else if (string.IsNullOrEmpty(Settings.AlyxLibDirectory))
        {
            ShowIntroAlyxLibPopup();
        }

        MenuBarRememberLastAddon.IsChecked = Settings.RememberLastAddon;

        // Command line load
        if (!string.IsNullOrEmpty(RequestedAddonName))
        {
            if (!HLA.AddonExists(RequestedAddonName))
            {
                ShowWarningPopup($"Folder '{RequestedAddonName}' is not a Half-Life Alyx addon!");
            }
            else
            {
                SelectAddon(RequestedAddonName);
            }
        }
        else
        {
            if (Settings.RememberLastAddon)
            {
                if (!string.IsNullOrEmpty(Settings.LastAddon))
                {
                    if (!SelectAddon(Settings.LastAddon))
                    {
                        DebugConsoleWarning($"Could not find last addon \"{Settings.LastAddon}\".");
                    }
                }
            }
        }

        var result = await GetAlyxLibVersionComparison();
        if (result.comparison > 0)
        {
            DebugConsoleInfo($"New version available!\nv{result.localVersion} -> v{result.remoteVersion}\nUse Help -> Check for Updates to download.");
        }
    }

    private void AddHyperlink(Paragraph paragraph, string linkText)
    {
        var hyperlink = new Hyperlink();
        var run = new Run { Text = linkText };
        hyperlink.Inlines.Add(run);

        bool isUrl = Uri.TryCreate(linkText, UriKind.Absolute, out Uri? uriResult) &&
                     (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        hyperlink.Click += async (s, e) =>
        {
            try
            {
                if (isUrl)
                {
                    // Open URL in browser
                    await Launcher.LaunchUriAsync(uriResult);
                }
                else if (Directory.Exists(linkText))
                {
                    // Open folder
                    await Launcher.LaunchFolderPathAsync(linkText);
                }
                else if (File.Exists(linkText))
                {
                    // Select file in explorer
                    Process.Start("explorer.exe", $"/select,\"{linkText}\"");
                }
            }
            catch (Exception ex)
            {
                // Optional: handle errors (log, show message, etc.)
            }
        };

        paragraph.Inlines.Add(hyperlink);
    }


    private Paragraph CreateRichParagraph(string message, Color color)
    {
        var paragraph = new Paragraph();

        string urlPattern = @"\b(?:https?://|www\.)\S+\b";
        string pathPattern = @"[A-Za-z]:\\(?:[^<>:""/\\|?*]+\\)*[^<>:""/\\|?*]*";

        var matches = Regex.Matches(message, $"{urlPattern}|{pathPattern}", RegexOptions.IgnoreCase);

        int lastIndex = 0;

        if (matches.Count == 0)
        {
            // No matches at all — just add full message
            paragraph.Inlines.Add(new Run { Text = message, Foreground = new SolidColorBrush(color) });
            return paragraph;
        }

        foreach (Match match in matches)
        {
            string candidate = match.Value;
            string validPath = Utils.FindLongestValidPath(candidate);

            if (validPath != null)
            {
                // Add plain text before the match
                if (match.Index > lastIndex)
                {
                    string plainText = message.Substring(lastIndex, match.Index - lastIndex);
                    paragraph.Inlines.Add(new Run
                    {
                        Text = plainText,
                        Foreground = new SolidColorBrush(color)
                    });
                }

                AddHyperlink(paragraph, validPath);

                // Add remaining text (candidate after validPath)
                string trailing = candidate.Substring(validPath.Length);
                if (!string.IsNullOrEmpty(trailing))
                    paragraph.Inlines.Add(new Run { Text = trailing, Foreground = new SolidColorBrush(color) });

                lastIndex = match.Index + candidate.Length;
            }
            else
            {
                // No valid path, just add as normal text
                paragraph.Inlines.Add(new Run { Text = candidate, Foreground = new SolidColorBrush(color) });
                lastIndex = match.Index + candidate.Length;
            }
        }

        return paragraph;
    }

    public async void WriteToDebugConsole(string message, Color color)
    {
        // Automatically log every debug console message to file
        FileLogger.Log(message);

        var paragraph = CreateRichParagraph(message, color);

        if (DebugConsole.Blocks.Count > 0)
        {
            paragraph.Margin = new Thickness(0, 4, 0, 0);
        }

        DebugConsole.Blocks.Add(paragraph);

        await Task.Delay(20);

        DispatcherQueue.TryEnqueue(() =>
        {

            DebugConsoleScroller.ScrollTo(DebugConsoleScroller.ScrollableWidth, DebugConsoleScroller.ScrollableHeight);
        });

        //WindowConsole.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string currentText);

        //WindowConsole.IsReadOnly = false;
        //WindowConsole.Document.Selection.StartPosition = currentText.Length;
        //WindowConsole.Document.Selection.EndPosition = currentText.Length;

        //if (!string.IsNullOrEmpty(currentText.Trim()))
        //{
        //    WindowConsole.Document.Selection.Text = "\r\n";
        //    WindowConsole.Document.Selection.StartPosition = WindowConsole.Document.Selection.EndPosition;
        //}
        //WindowConsole.Document.Selection.Text = message;
        //WindowConsole.Document.Selection.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Yellow;

        ////WindowConsole.Document.Selection.Text = "\r\n";
        //WindowConsole.IsReadOnly = true;

        ////WindowConsole.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, WindowConsole.Document.GetText() + "\n" + message);
        ////WindowConsole.TextDocument.
    }

    /// <summary>
    /// The current local addon selected, or null otherwise.
    /// </summary>
    private LocalAddon? CurrentAddon = null;

    ///// <summary>
    ///// Check files for the VScript install option and show any warnings.
    ///// </summary>
    //private void CheckVScriptOption()
    //{
    //    if (CurrentAddon == null) return;

    //    (AlyxLib.FileCheckResult Result, string Message, InfoBarSeverity Severity) = AlyxLib.CheckVScriptFiles(CurrentAddon);

    //    if (Result == AlyxLib.FileCheckResult.AlyxLibPathNotFound)
    //    {
    //        SetAlyxLibPath(null);
    //        return;
    //    }

    //    if (Result == AlyxLib.FileCheckResult.Error)
    //    {
    //        InstallOptionScriptBase.ShowInfoBar(Severity, Message);
    //        return;
    //    }

    //    InstallOptionScriptBase.HideInfoBar();

    //    if (Result == AlyxLib.FileCheckResult.FullyInstalled)
    //    {
    //        InstallOptionScriptBase.IsChecked = true;
    //    }
    //    else if (Result == AlyxLib.FileCheckResult.PartiallyInstalled)
    //    {
    //        InstallOptionScriptBase.IsChecked = null;
    //    }
    //    else
    //    {
    //        InstallOptionScriptBase.IsChecked = false;
    //    }

    //    //
    //}

    private void CheckAllOptions(bool showWarnings = false)
    {
        if (CurrentAddon == null) return;

        //AlyxLibInstallOption[] options = [
        //    InstallOptionScriptBase,
        //    InstallOptionSoundEvent,
        //    InstallOptionPanorama,
        //    InstallOptionGit
        //    ];
        Dictionary<AlyxLibInstallOption, Func<LocalAddon, (AlyxLib.FileCheckResult Result, string Message, InfoBarSeverity Severity)>> checks = new() {
            { InstallOptionScriptBase, AlyxLibInstance.FileManager.CheckVScriptFiles },
            { InstallOptionSoundEvent, AlyxLibInstance.FileManager.CheckSoundEventFiles },
            { InstallOptionPanorama, AlyxLibInstance.FileManager.CheckPanoramaFiles },
            { InstallOptionGit, AlyxLibInstance.FileManager.CheckGit }
        };

        foreach (var (option, check) in checks)
        {
            var (Result, Message, Severity) = check(CurrentAddon);
            if (Result == FileCheckResult.AlyxLibPathNotFound)
            {
                return;
            }
            else if (Result == FileCheckResult.Error || (Result == FileCheckResult.Warning && showWarnings))
            {
                option.ShowInfoBar(Severity, Message);
                continue;
            }

            InstallOptionScriptBase.HideInfoBar();
        }
    }

    /// <summary>
    /// Set the path to AlyxLib. Controls will be updated based on whether the path is correct.
    /// </summary>
    /// <param name="path">Path of the AlyxLib directory</param>
    private void SetAlyxLibPath(string? path, bool silent = false)
    {
        bool found = false;
        if (path != null)
        {
            if (AlyxLibHelpers.CheckPathIsAlyxLib(path))
            {
                found = true;
                AlyxLibInstance.SetAlyxLibPath(path);
                if (!silent)
                    DebugConsoleSuccess($"AlyxLib path set to {path}");
            }
        }
        AlyxLibNotFoundInfoBar.IsOpen = !found;
        UpdateEnabledControlsBasedOnCorrectSettings();
    }

    private void ResetAllOptions()
    {
        InstallOptionScriptBase.IsChecked = false;
        ScriptEditorSettingsOption.SelectedIndex = 0;
        InstallOptionSoundEvent.IsChecked = false;
        InstallOptionPanorama.IsChecked = false;
        InstallOptionGit.IsChecked = false;
        AddonModNameTextBox.Text = "";
    }

    private void UpdateEnabledControlsBasedOnCorrectSettings()
    {
        bool addonIsValid = CurrentAddon != null;
        InstallButton.IsEnabled = addonIsValid && AlyxLibInstance.AlyxLibExists && StringIsValidModName(AddonModNameTextBox.Text);
        UninstallButton.IsEnabled = addonIsValid;

        AddonModNameTextBox.IsEnabled = addonIsValid;
        //InstallOptionVSCodeSettings.IsEnabled = addonIsValid;
        InstallOptionScriptBase.IsEnabled = addonIsValid;
        InstallOptionSoundEvent.IsEnabled = addonIsValid;
        InstallOptionPanorama.IsEnabled = addonIsValid;
        InstallOptionGit.IsEnabled = addonIsValid;
        ScriptEditorSettingsOption.IsEnabled = addonIsValid;
    }

    private void ShowInfoBar(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        AlyxLibTopInfoBar.Message = message;
        AlyxLibTopInfoBar.Severity = severity;
        AlyxLibTopInfoBar.IsOpen = true;
    }

    private void HideInfoBar()
    {
        AlyxLibTopInfoBar.IsOpen = false;
    }

    /// <summary>
    /// Select the current addon to be used when managing AlyxLib.
    /// </summary>
    /// <param name="addonName"></param>
    /// <returns></returns>
    internal bool SelectAddon(string addonName)
    {
        CurrentAddon = HLA.GetAddon(addonName);
        UpdateEnabledControlsBasedOnCorrectSettings();
        if (CurrentAddon != null)
        {

            //if (AlyxLib.AddonHasAlyxLib(CurrentAddon))
            //{
            //    // If AlyxLib exists in some form, check if it is a pre-installer version
            //    if (!AlyxLib.AddonHasConfig(CurrentAddon))
            //    {
            //        ShowWarningPopup($"It looks like {CurrentAddon.Name} has a version of AlyxLib that wasn't installed using this installer or the config file was deleted. Some options may appear incorrectly. It is recommended to backup your project before installing in case any custom AlyxLib files are modified.");
            //    }
            //    // Otherwise populate the options
            //    else
            //    {
            //        AlyxLibAddonConfig config = AlyxLib.GetAddonConfig(CurrentAddon);
            //        InstallOptionScriptBase.IsChecked = config.VScriptInstalled;
            //        InstallOptionSoundEvent.IsChecked = config.SoundEventInstalled;
            //        InstallOptionPanorama.IsChecked = config.PanoramaInstalled;
            //        InstallOptionGit.IsChecked = config.GitInstalled;
            //        AddonModNameTextBox.Text = config.ModFolderName;
            //        InstallOptionVSCodeSettings.IsChecked = config.UseAlyxLibVSCodeSettings;
            //    }

            //    // Show any errors for each option
            //    CheckAllOptions();
            //}

            ResetAllOptions();

            if (AlyxLibHelpers.AddonHasConfig(CurrentAddon, out AddonConfig config))
            {
                InstallOptionScriptBase.IsChecked = config.VScriptInstalled;
                InstallOptionSoundEvent.IsChecked = config.SoundEventInstalled;
                InstallOptionPanorama.IsChecked = config.PanoramaInstalled;
                InstallOptionGit.IsChecked = config.GitInstalled;
                AddonModNameTextBox.Text = config.ModFolderName;
                ScriptEditorSettingsOption.SelectedIndex = ScriptEditorOptions.FindIndex(x => x.Item2 == config.EditorType);

                // Show any errors
                CheckAllOptions();

                DebugConsoleVerbose($"AlyxLib config file found for {CurrentAddon.Name}.");
            }
            // No settings but AlyxLib exists in some form, probably a pre-installer version
            else if (AlyxLibHelpers.AddonHasAlyxLib(CurrentAddon))
            {
                DebugConsoleVerbose($"AlyxLib config file wasn't found for {CurrentAddon.Name}, but AlyxLib was detected in addon.");
                ShowWarningPopup($"It looks like {CurrentAddon.Name} has a version of AlyxLib that wasn't installed using this installer or the config file was deleted. Some options may appear incorrectly. It is recommended to backup your project before installing in case any custom AlyxLib files are modified.");

                // Show any errors
                CheckAllOptions();
            }
            else
            {
                // Show any errors or warnings for file collisions
                CheckAllOptions(true);

                DebugConsoleVerboseWarning($"AlyxLib config file wasn't found for {CurrentAddon.Name}.");
            }

            AppTitle.Text = $"AlyxLib Installer: {addonName}";

            UpdateEnabledControlsBasedOnCorrectSettings();

            if (Settings.RememberLastAddon)
            {
                Settings.LastAddon = addonName;
            }

            foreach (RadioMenuFlyoutItem addonFlyout in MenuBarAddons.Items.Cast<RadioMenuFlyoutItem>())
            {
                if (addonFlyout.Text == addonName)
                {
                    addonFlyout.IsChecked = true;
                    break;
                }
            }

            AddonModNameTextBox.PlaceholderText = CurrentAddon.Name;

            return true;
        }
        else
        {
            return false;
        }
    }

    private void UnselectAddon()
    {
        CurrentAddon = null;
        AddonModNameTextBox.Text = "";
        //InstallOptionVSCodeSettings.IsChecked = false;
        InstallOptionScriptBase.IsChecked = false;
        InstallOptionSoundEvent.IsChecked= false;
        InstallOptionPanorama.IsChecked = false;
        InstallOptionGit.IsChecked = false;

        UpdateEnabledControlsBasedOnCorrectSettings();

        foreach (RadioMenuFlyoutItem addonFlyout in MenuBarAddons.Items.Cast<RadioMenuFlyoutItem>())
        {
            addonFlyout.IsChecked = false;
        }
        UpdateEnabledControlsBasedOnCorrectSettings();
    }

    /// <summary>
    /// Open a folder select dialog for the user to choose where AlyxLib is installed.
    /// </summary>
    private async void PromptUserToSelectAlyxLibFolder()
    {

        // Create a folder picker
        FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

        // See the sample code below for how to make the window accessible from the App class.
        var window = App.MainWindow;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the folder picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add("*");

        // Open the picker for the user to pick a folder
        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            if (AlyxLibHelpers.CheckPathIsAlyxLib(folder.Path))
            {
                //var t = folder.Path;
                //var g = StorageApplicationPermissions.FutureAccessList.ContainsItem("PickedFolderToken");
                //StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                SetAlyxLibPath(folder.Path);
            }
            else
            {
                SetAlyxLibPath(null);
                ShowWarningPopup($"{folder.Path} is not a valid AlyxLib folder!");
            }
        }

    }

    private void SetLoadingOverlayVisible(bool visible, string? text = null)
    {
        LoadingOverlay.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        DownloadProgressRing.IsIndeterminate = true;
        DownloadProgressRing.Value = 0;
        DownloadProgressRing.IsActive = visible;
        if (text != null)
        {
            DownloadProgressText.Text = text;
        }
    }

    private async void DownloadAlyxLib(bool newLocation = false)
    {
        string downloadPath;

        if (!AlyxLibInstance.AlyxLibExists || newLocation)
        {
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder == null)
            {
                DebugConsoleWarning("User did not select a folder, download cancelled");
                return;
            }

            downloadPath = folder.Path;
            var extractPathInfo = new PathInfo(downloadPath);
            if (extractPathInfo.Name != "alyxlib")
            {
                downloadPath = Path.Combine(downloadPath, "alyxlib");
            }
            Directory.CreateDirectory(downloadPath);
        }
        else
        {
            downloadPath = AlyxLibInstance.AlyxLibPath.FullName;
        }

        SetLoadingOverlayVisible(true, "Downloading AlyxLib");

        DebugConsoleVerbose("Downloading AlyxLib...");
        await AlyxLibHelpers.DownloadRepository(downloadPath, Progress_ProgressChanged);
        SetAlyxLibPath(downloadPath);

        SetLoadingOverlayVisible(false);

        var versionSuccess = AlyxLibInstance.VersionManager.TryGetLocalVersion(out string localVersion);
        if (versionSuccess)
        {
            DebugConsoleSuccess($"AlyxLib v{localVersion} downloaded successfully!");
        }
        else
        {
            DebugConsoleSuccess($"AlyxLib downloaded successfully!");
        }
    }

    private void Progress_ProgressChanged(object? sender, float e)
    {
        DownloadProgressRing.IsIndeterminate = false;
        DebugConsoleVerbose($"{e}");
        DebugConsoleMessage("Hmmm hello");
        DownloadProgressRing.Value = e;
    }

    public async Task<ContentDialogResult> GetPopupResult(string message, string title = "", string primaryButtonText = "", string closeButtonText = "Cancel")
    {
        ContentDialog dialog = new ContentDialog();

        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
        dialog.XamlRoot = this.Content.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = title;
        dialog.CloseButtonText = closeButtonText;
        dialog.PrimaryButtonText = primaryButtonText;

        // Default button based on if primary button exists
        dialog.DefaultButton = string.IsNullOrWhiteSpace(primaryButtonText) ? ContentDialogButton.Close : ContentDialogButton.Primary;

        dialog.Content = new SimpleTextDialog(message);

        var result = await dialog.ShowAsync();
        return result;
    }

    /// <summary>
    /// Show a simple popup with a message and an OK button.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="title"></param>
    public async void ShowSimplePopup(string message, string title = "")
    {
        await GetPopupResult(message, title, "OK", "");
    }

    public void ShowWarningPopup(string message)
    {
        //ContentDialog dialog = new ContentDialog();

        //// XamlRoot must be set in the case of a ContentDialog running in a Desktop app
        //dialog.XamlRoot = this.Content.XamlRoot;
        //dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        //dialog.Title = "Warning";
        //dialog.CloseButtonText = "OK";
        //dialog.DefaultButton = ContentDialogButton.Close;

        //dialog.Content = new SimpleTextDialog(message);

        //var result = await dialog.ShowAsync();

        ShowSimplePopup(message, "Warning");
    }

    /// <summary>
    /// Shows the popup asking the user to download or select AlyxLib location.
    /// </summary>
    public async void ShowIntroAlyxLibPopup()
    {
        ContentDialog dialog = new ContentDialog();

        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
        dialog.XamlRoot = this.Content.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = "Download AlyxLib";
        dialog.PrimaryButtonText = "Download AlyxLib";
        dialog.SecondaryButtonText = "Select AlyxLib Folder";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;

        dialog.Content = new SimpleTextDialog("AlyxLibDownloadPopup_Message".GetLocalized());

        var result = await dialog.ShowAsync();

        switch (result)
        {
            case ContentDialogResult.Primary:
                DownloadAlyxLib();
                break;

            case ContentDialogResult.Secondary:
                PromptUserToSelectAlyxLibFolder();
                break;
        }
    }

    public async void ShowPrivilegeWarningPopup()
    {
        ContentDialog dialog = new ContentDialog();
        dialog.XamlRoot = this.Content.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = "Administrator Privileges Required";
        dialog.CloseButtonText = "OK";
        dialog.DefaultButton = ContentDialogButton.Close;
        dialog.Content = new PrivilegeWarningDialog();
        await dialog.ShowAsync();
    }

    private void MenuBarAddonSelection_Click(object sender, RoutedEventArgs e)
    {
        var addonFlyout = (RadioMenuFlyoutItem)sender;
        if (SelectAddon(addonFlyout.Text))
        {
            addonFlyout.IsChecked = true;
        }
        else
        {
            ShowWarningPopup($"Could not find addon {addonFlyout.Text} for some reason!");
        }
    }

    private void MenuBarThemes_Click(object sender, RoutedEventArgs e)
    {

        var themeFlyout = (RadioMenuFlyoutItem)sender;
        if (Enum.TryParse(themeFlyout.Text, out ElementTheme theme))
        {
            RootGrid.RequestedTheme = theme;
            Settings.Theme = theme.ToString();
        }
    }

    private void AddonModName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (AddonModNameTextBox.Text == "Test")
        {
            InstallOptionSoundEvent.ShowInfoBar(InfoBarSeverity.Warning, "A custom manifest for this addon already exists, this option will be skipped.");
        }
    }

    private void UninstallButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        if (CurrentAddon == null)
        {
            DebugConsoleError("No addon selected, cannot remove AlyxLib for upload");
            return;
        }

        string modFolderName = AddonModNameTextBox.Text;
        if (modFolderName == "")
        {
            modFolderName = CurrentAddon.Name;
        }

        SetLoadingOverlayVisible(true, "Removing AlyxLib files for workshop upload");

        AlyxLibInstance.FileManager.UninstallAlyxLibForUpload(CurrentAddon, new()
        {
            VScriptInstalled = InstallOptionScriptBase.IsChecked == true,
            SoundEventInstalled = InstallOptionSoundEvent.IsChecked == true,
            PanoramaInstalled = InstallOptionPanorama.IsChecked == true,
            GitInstalled = InstallOptionGit.IsChecked == true,
            ModFolderName = modFolderName,
            EditorType = (ScriptEditor)ScriptEditorSettingsOption.SelectedValue,

            Version = AlyxLibInstance.AlyxLibVersion
        });

        SetLoadingOverlayVisible(false);

        ShowInfoBar("Remember to Install again after uploading your addon to the workshop!");
    }

    // Add event handlers for the new uninstall dropdown options
    private async void UninstallRemoveUnchanged_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog();

        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
        dialog.XamlRoot = this.Content.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = "Remove AlyxLib From Addon?";
        dialog.PrimaryButtonText = "Remove All";
        dialog.SecondaryButtonText = "SymLinks Only";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;

        //dialog.PrimaryButtonStyle = Application.Current.Resources["CriticalRedButtonStyle"] as Style;
        //dialog.SecondaryButtonStyle = Application.Current.Resources["CriticalRedButtonStyle"] as Style;
        dialog.Content = new UninstallDialog() { ExpanderContent = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10" };

        var result = await dialog.ShowAsync();
    }


    private void InstallOptionScriptBase_Click(object sender, RoutedEventArgs e)
    {
    }

    private void MenuBarSelectAlyxLibPath_Click(object sender, RoutedEventArgs e)
    {
        PromptUserToSelectAlyxLibFolder();
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        //File.Create(Path.Combine(AlyxLib.AlyxLibPath.FullName, "testfile.txt"));
        if (CurrentAddon == null)
        {
            DebugConsoleError("No addon selected, cannot install AlyxLib");
            return;
        }

        HideInfoBar();

        string modFolderName = AddonModNameTextBox.Text;
        if (modFolderName == "")
        {
            modFolderName = CurrentAddon.Name;
        }

        SetLoadingOverlayVisible(true, "Installing AlyxLib");

        AlyxLibInstance.FileManager.InstallAlyxLib(CurrentAddon, new()
        {
            VScriptInstalled = InstallOptionScriptBase.IsChecked == true,
            SoundEventInstalled = InstallOptionSoundEvent.IsChecked == true,
            PanoramaInstalled = InstallOptionPanorama.IsChecked == true,
            GitInstalled = InstallOptionGit.IsChecked == true,
            ModFolderName = modFolderName,
            EditorType = (ScriptEditor)ScriptEditorSettingsOption.SelectedValue,

            Version = AlyxLibInstance.AlyxLibVersion
        });

        SetLoadingOverlayVisible(false);
    }

    private void MenuBarRememberLastAddon_Click(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleMenuFlyoutItem)sender;
        Settings.RememberLastAddon = toggle.IsChecked;
        Settings.LastAddon = Settings.RememberLastAddon ? CurrentAddon?.Name ?? "" : "";
    }

    private void MenuBarVerboseLogging_Click(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleMenuFlyoutItem)sender;
        Settings.VerboseConsole = toggle.IsChecked;
    }

    private void MenuBarOpenWiki_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://github.com/FrostSource/alyxlib/wiki", UseShellExecute = true });
    }

    private void MenuBarOpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://github.com/FrostSource/alyxlib", UseShellExecute = true });
    
    }

    private void MenuBarDownloadAlyxLib_Click(object sender, RoutedEventArgs e)
    {
        DownloadAlyxLib(newLocation: true);
    }

    /// <summary>
    /// Get the version comparison between the local and remote AlyxLib versions.
    /// > 0 = Local is older
    /// = 0 = Same version
    /// < 0 = Local is newer
    /// </summary>
    /// <returns></returns>
    private async Task<(int comparison, SemVersion localVersion, SemVersion remoteVersion)> GetAlyxLibVersionComparison()
    {
        try
        {
            return await AlyxLibInstance.VersionManager.CompareVersions();
        }
        catch (Exception ex)
        {
            DebugConsoleError(ex.Message);
            return (0, new SemVersion(0), new SemVersion(0));
        }
    }

    private async void MenuBarCheckForUpdates_Click(object sender, RoutedEventArgs e)
    {

        try
        {
            var (versionComparison, localVersion, remoteVersion) = await AlyxLibInstance.VersionManager.CompareVersions();
            DebugConsoleVerbose($"Version diff: {versionComparison}");
            if (versionComparison > 0)
            {
                var result = await GetPopupResult($"New version available!\nv{localVersion} -> v{remoteVersion}", "Update Available", "Download", "Cancel");
                if (result == ContentDialogResult.Primary)
                {
                    DownloadAlyxLib();
                }
            }
            else if (versionComparison == 0)
            {
                ShowSimplePopup($"You are up to date!\nv{localVersion}", "Up To Date");
            }
            else
            {
                ShowSimplePopup("You are ahead of the latest version somehow, you must have a pre-release version.");
            }
        }
        catch (Exception ex)
        {
            DebugConsoleError(ex.Message);
        }


    }

    private static bool StringIsValidModName(string input)
    {
        return !input.Any(c => !char.IsLetterOrDigit(c) && c != '_');
    }

    private void AddonModNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox != null)
        {
            //string input = textBox.Text;

            //string filtered = new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            //if (input != filtered)
            //{
            //    textBox.Text = filtered;

            //    textBox.SelectionStart = filtered.Length;
            //}

            if (!StringIsValidModName(textBox.Text))
            {
                AddonModNameErrorText.Text = "Invalid name! Must contain only letters, numbers and underscore characters.";
                //AddonModNameErrorText.Visibility = Visibility.Visible;
                AddonModNameErrorText.Opacity = 1;
            }
            else
            {
                AddonModNameErrorText.Text = "";
                //AddonModNameErrorText.Visibility = Visibility.Collapsed;
                AddonModNameErrorText.Opacity = 0;
            }
            UpdateEnabledControlsBasedOnCorrectSettings();
        }
    }

    private void MenuBarOpenAlyxLibPath_Click(object sender, RoutedEventArgs e)
    {
        if (AlyxLibInstance.AlyxLibPath == null)
        {
            ShowWarningPopup("AlyxLib path is not set!");
            return;
        }

        if (!Directory.Exists(AlyxLibInstance.AlyxLibPath.FullName))
        {
            DebugConsoleError("AlyxLib path does not exists for some reason!");
            return;
        }

        Process.Start("explorer.exe", AlyxLibInstance.AlyxLibPath.FullName);
    }

    private void UpdateMenuBarAddonsList()
    {
        MenuBarAddons.Items.Clear();

        foreach (var addonName in HLA.GetAddonNames())
        {
            var newAddonFlyoutItem = new RadioMenuFlyoutItem();
            newAddonFlyoutItem.Text = addonName;
            newAddonFlyoutItem.Click += MenuBarAddonSelection_Click;
            newAddonFlyoutItem.GroupName = "addons";
            MenuBarAddons.Items.Add(newAddonFlyoutItem);
        }
    }

    //private void MenuBarAddons_Opening(object? sender, object e)
    //{
    //    UpdateMenuBarAddonsList();
    //}

    //private void MenuBarAddons_PointerEntered(object sender, PointerRoutedEventArgs e)
    //{
    //    UpdateMenuBarAddonsList();
    //}

    private void MenuBarAddons_GotFocus(object sender, RoutedEventArgs e)
    {
        UpdateMenuBarAddonsList();
    }

    //private void MenuBarItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    //{
    //    UpdateMenuBarAddonsList();
    //}

    //private void MenuBarAddons_FocusEngaged(Control sender, FocusEngagedEventArgs args)
    //{
    //    UpdateMenuBarAddonsList();
    //}

    //private void AddonModNameTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    //{
    //    bool isLetter = (e.Key >= Windows.System.VirtualKey.A && e.Key <= Windows.System.VirtualKey.Z);

    //    bool isUnderscore 
    //}

    private const string ContextMenuKey = @"Software\Classes\Directory\shell\AlyxLibInstaller";
    private const string ContextMenuCommandKey = @"Software\Classes\Directory\shell\AlyxLibInstaller\command";
    private const string BackgroundContextMenuKey = @"Software\Classes\Directory\Background\shell\AlyxLibInstaller";
    private const string BackgroundContextMenuCommandKey = @"Software\Classes\Directory\Background\shell\AlyxLibInstaller\command";

    private void MenuBarAddContextMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "AlyxLibInstaller.exe";

            // Folder context menu
            using (var key = Registry.CurrentUser.CreateSubKey(ContextMenuKey))
            {
                key.SetValue("", "Open with AlyxLibInstaller");
                key.SetValue("Icon", exePath);
            }
            using (var commandKey = Registry.CurrentUser.CreateSubKey(ContextMenuCommandKey))
            {
                commandKey.SetValue("", $"\"{exePath}\" \"%V\"");
            }

            // Background context menu
            using (var key = Registry.CurrentUser.CreateSubKey(BackgroundContextMenuKey))
            {
                key.SetValue("", "Open with AlyxLibInstaller");
                key.SetValue("Icon", exePath);
            }
            using (var commandKey = Registry.CurrentUser.CreateSubKey(BackgroundContextMenuCommandKey))
            {
                commandKey.SetValue("", $"\"{exePath}\" \"%V\"");
            }

            ShowSimplePopup("Context menu added! Right-click any folder or blank space in a folder to use AlyxLibInstaller.", "Success");
        }
        catch (Exception ex)
        {
            ShowWarningPopup("Failed to add context menu:\n" + ex.Message);
        }
    }

    private void MenuBarRemoveContextMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(ContextMenuKey, false);
            Registry.CurrentUser.DeleteSubKeyTree(BackgroundContextMenuKey, false);
            ShowSimplePopup("Context menu removed.", "Success");
        }
        catch (Exception ex)
        {
            ShowWarningPopup("Failed to remove context menu:\n" + ex.Message);
        }
    }

    private async void OpenAddonFolder_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentAddon == null) return; //TODO: Show warning?

        if (!Directory.Exists(CurrentAddon.ContentPath)) return;

        await Launcher.LaunchFolderPathAsync(CurrentAddon.ContentPath);
    }

    private void ClearDebugConsole_Click(object sender, RoutedEventArgs e)
    {
        DebugConsole.Blocks.Clear();
    }
}
