using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AlyxLibInstallerWPF;

/// <summary>
/// WPF does not have ElementTheme so we add it for Fluent
/// </summary>
public enum ElementTheme
{
    Default, // System
    Light,
    Dark
}

public partial class MainWindow : Window
{
    //private readonly AlyxLibManager AlyxLibInstance;
    //private readonly Settings Settings = SettingsManager.Settings;

    //public List<Tuple<string, ElementTheme>> ElementThemeOptions { get; } =
    //Enum.GetValues(typeof(ElementTheme))
    //    .Cast<ElementTheme>()
    //    .Select(theme => new Tuple<string, ElementTheme>(theme.ToString(), theme))
    //    .ToList();
    //public List<Tuple<string, ElementTheme>> ElementThemeOptions { get; } =
    //[.. Enum.GetValues<ElementTheme>()
    //    .Cast<ElementTheme>()
    //    .Select(theme => new Tuple<string, ElementTheme>(theme.ToString(), theme))];

    //public List<Tuple<string, ScriptEditor>> ScriptEditorOptions { get; } =
    //[
    //    new Tuple<string, ScriptEditor>("None", ScriptEditor.None),
    //    new Tuple<string, ScriptEditor>("VS Code", ScriptEditor.VisualStudioCode),
    //];

    //public AlyxLibInstallerSharedViewModel ViewModel { get; } = new();
    //private Settings Settings => ViewModel.Settings;

    //public string? RequestedAddonName { get; set; }

    //public IRelayCommand SelectThemeCommand { get; }

    ///// <summary>
    ///// The current local addon selected, or null otherwise.
    ///// </summary>
    //private LocalAddon? CurrentAddon = null;

    public MainWindow()
    {
        InitializeComponent();
        // Center window
        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

        //AlyxLibInstance = new() { Logger = new AlyxLibInstallerLogger(this) };

        //var viewModel = (AlyxLibInstallerSharedViewModel)DataContext;
        //var viewModel = App.Services.GetRequiredService<AlyxLibInstallerSharedViewModel>();
        //var viewModel = new AlyxLibInstallerSharedViewModel();
        //DataContext = ViewModel;
        //DataContext = viewModel;

        //UpdateMenuBarAddonsList();

        Loaded += (_, _) => MainWindow_Activated_FirstTime();



        //ViewModel.PropertyChanged += async (object? sender, PropertyChangedEventArgs e) =>
        //{
        //    if (e.PropertyName == nameof(ViewModel.CurrentTheme))
        //    {
        //        await Task.Yield();
        //        var converter = new Converters.AppThemeToFluentThemeConverter();
        //        var newTheme = (ThemeMode)converter.Convert(ViewModel.CurrentTheme, typeof(ThemeMode), null, null);
        //        if (ThemeMode == newTheme) return;
        //        ThemeMode = newTheme;
        //    }
        //};
        //ThemeMode = viewModel.CurrentTheme.ToThemeMode();
        //viewModel.OnThemeChanged += async (object? sender, AppTheme theme) =>
        //{
        //    DebugConsoleInfo($"Theme changed to {theme}");
        //    await Task.Yield();
        //    //var converter = new Converters.AppThemeToFluentThemeConverter();
        //    //var newTheme = (ThemeMode)converter.Convert(theme, typeof(ThemeMode), null, null);
        //    var newTheme = theme.ToThemeMode();
        //    if (ThemeMode == newTheme) return;
        //    ThemeMode = newTheme;
        //};

        // moved to viewmodel can delete
        //WeakReferenceMessenger.Default.Register<AddonChangedMessage>(this, (r, m) =>
        //{
        //    if (m.Value is AddonChangedPayload payload)
        //    {
        //        if (payload.IsDefaultConfig && payload.AddonHasAlyxLib)
        //            DialogHelper.ShowWarningPopup(this, $"It looks like {payload.NewAddon.Name} has a version of AlyxLib that wasn't installed using this installer or the config file was deleted. Some options may appear incorrectly. It is recommended to backup your project before installing in case any custom AlyxLib files are modified.");
        //    }
        //});

    }

    //TODO: DELETE AFTER INFOBAR SET UP
    private void InfoBar_CloseButtonClick(object sender, RoutedEventArgs e)
    {
        // Handle close event
        System.Diagnostics.Debug.WriteLine("InfoBar closed by user");
        DialogHelper.ShowWarningPopup(this, "InfoBar closed by user");
    }

    private void MainWindow_Activated_FirstTime()
    {

        // WPF console starts with paragraphs by default so clear it
        ///TODO Make sure this plays nice with viewmodel, move to viewmodel message/event
        DebugConsole.Document.Blocks.Clear();

        //FileGlobCollection globCollection = ["My","Single","Test"];
        //globCollection.Clear();
        //globCollection.Add("maps/");
        //globCollection.Add("save/");
        //var myresult = await DialogHelper.ShowCustomPopupAsync(this, new UploadRemovalListView(globCollection.Clone(), HLA.GetAddon("akimbo")));

        //var newGlob = myresult?.Data?.GlobCollection;

        //if (newGlob != null)
        //{
        //    DebugConsoleInfo("CURRENT:");
        //    foreach (var glob in globCollection) DebugConsoleInfo(glob.Name);
        //    DebugConsoleInfo("NEW:");
        //    foreach (var glob in newGlob) DebugConsoleInfo(glob.Name);
        //    DebugConsoleInfo("FILES:");
        //    foreach (var file in newGlob.GetMatchingFiles(@"C:\Program Files (x86)\Steam\steamapps\common\Half-Life Alyx\game\hlvr_addons\akimbo")) DebugConsoleInfo(file);
        //}
    }

    private void UninstallSplitButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // prevents bubbling to main button

        if (((Button)sender).ContextMenu != null)
        {
            ((Button)sender).ContextMenu.PlacementTarget = (Button)sender;
            ((Button)sender).ContextMenu.IsOpen = true;
        }
    }

    private void UninstallRemoveAllFiles_Click(object sender, RoutedEventArgs e)
    {
        //if (UninstallButton is FrameworkElement anchor)
        //{
        //    var flyout = new Flyout
        //    {
        //        Content = new StackPanel
        //        {
        //            Children =
        //            {
        //                new TextBlock { Text = "All AlyxLib files will be removed. Proceed?" },
        //                new Button { Content = "Yes", Margin = new Thickness(0,8,0,0), HorizontalAlignment = HorizontalAlignment.Left }
        //            }
        //        }
        //    };
        //    flyout.ShowAt(anchor);
        //}

        if (RemoveForUploadButton is FrameworkElement anchor)
        {
            var popup = new Popup
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = false,
                PopupAnimation = PopupAnimation.Fade
            };

            var yesButton = new Button
            {
                Content = "Yes",
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            yesButton.Click += (_, __) =>
            {
                // Close popup
                popup.IsOpen = false;

                // (Optional) cleanup event handler so GC can collect everything
                yesButton.Click -= (_, __) => { };

                // Do your uninstall logic here
                MessageBox.Show("Removing all AlyxLib files...");
            };

            popup.Child = new Border
            {
                Background = (Brush)Application.Current.Resources["ControlFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["ControlFillColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Child = new StackPanel
                {
                    Children =
                {
                    new TextBlock
                    {
                        Text = "All AlyxLib files will be removed. Proceed?",
                        Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                    },
                    yesButton
                }
                }
            };

            popup.IsOpen = true;
        }

    }

    private void ClearDebugConsole_Click(object sender, RoutedEventArgs e)
    {
        DebugConsole.Document.Blocks.Clear();
    }

    private void MenuBarExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        ////DebugConsoleInfo(ViewModel.IsFolderNameInvalid.ToString());
        ////InstallOptionSoundEvent.IsChecked = null;
        //ViewModel.CurrentTheme = AppTheme.Light;
        //DebugConsoleInfo((((AlyxLibInstallerSharedViewModel)DataContext).AddonConfig.ModFolderName == string.Empty).ToString());

        //var dc = ((AlyxLibInstallerSharedViewModel)DataContext);
        //DebugConsoleInfo($"{dc.IsInstallationReady}");
        //DebugConsoleInfo($"{dc.AlyxLibInstance.AlyxLibExists}");
        //DebugConsoleInfo($"{dc.SelectedAddon != null}");
        //DebugConsoleInfo($"{dc.AddonConfig != null}");
        //DebugConsoleInfo($"{!dc.IsFolderNameInvalid}");

        DebugConsole.ScrollToEnd();
        //DebugConsole.ScrollToVerticalOffset(double.MaxValue);
    }

    private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        e.Handled = NumericTextBoxRegex().IsMatch(e.Text);
    }

    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumericTextBoxRegex();

    //private async void MenuBarManageRemoveList_Click(object sender, RoutedEventArgs e)
    //{
    //    await DialogHelper.ShowCustomPopupAsync(this, new UploadRemovalListView(), "Manage Removal List", "Save", null, "Cancel");
    //}
}