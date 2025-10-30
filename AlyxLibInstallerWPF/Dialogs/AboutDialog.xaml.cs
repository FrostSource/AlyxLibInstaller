using System.Windows.Controls;
using System.Windows.Navigation;

namespace AlyxLibInstallerWPF.Dialogs;
/// <summary>
/// Interaction logic for AboutView.xaml
/// </summary>
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }

    //private void Hyperlink_Click(object sender, RoutedEventArgs e)
    //{
    //    var thing = e;
    //    //AlyxLibInstallerShared.Launcher.Launch(e.Uri.AbsoluteUri);
    //}

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        AlyxLibInstallerShared.Launcher.Launch(e.Uri.AbsoluteUri);
    }
}
