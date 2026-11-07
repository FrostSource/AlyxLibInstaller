using AlyxLibInstallerShared;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace AlyxLibInstallerWPF.Dialogs;

public partial class WorkshopPropertiesView : UserControl
{
    public WorkshopPropertiesView()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Launcher.Launch(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}
