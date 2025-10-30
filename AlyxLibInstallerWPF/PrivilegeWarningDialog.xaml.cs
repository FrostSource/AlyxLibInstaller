using AlyxLib;
using System.Windows;
using System.Windows.Controls;

namespace AlyxLibInstallerWPF;
/// <summary>
/// Interaction logic for PrivilegeWarningDialog.xaml
/// </summary>
public partial class PrivilegeWarningDialog : UserControl
{
    public PrivilegeWarningDialog()
    {
        InitializeComponent();
    }
    private void OpenDeveloperModeSettings_Click(object sender, RoutedEventArgs e)
    {
        PrivilegeChecker.OpenDeveloperSettings();
    }
}
