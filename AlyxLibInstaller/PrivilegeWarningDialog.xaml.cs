using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

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
