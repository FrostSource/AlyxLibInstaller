using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace AlyxLibInstaller;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PrivilegeWarningDialog : Page
{
    public PrivilegeWarningDialog()
    {
        InitializeComponent();
    }

    private void OpenDeveloperModeSettings_Click(object sender, RoutedEventArgs e)
    {
        // Opens the Windows Developer Mode settings page
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:developers",
            UseShellExecute = true
        });
    }
}
