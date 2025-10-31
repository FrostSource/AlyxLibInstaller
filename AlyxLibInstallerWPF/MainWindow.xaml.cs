using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace AlyxLibInstallerWPF;

public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => MainWindow_Activated_FirstTime();
    }

    private void MainWindow_Activated_FirstTime()
    {

        // WPF console starts with paragraphs by default so clear it
        ///TODO Make sure this plays nice with viewmodel, move to viewmodel message/event
        DebugConsole.Document.Blocks.Clear();
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

    private void ClearDebugConsole_Click(object sender, RoutedEventArgs e)
    {
        DebugConsole.Document.Blocks.Clear();
    }

    private void MenuBarExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumericTextBoxRegex();

    private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        e.Handled = NumericTextBoxRegex().IsMatch(e.Text);
    }
}