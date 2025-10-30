using AlyxLibInstallerShared.Models;
using Source2HelperLibrary;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace AlyxLibInstallerWPF;
/// <summary>
/// Interaction logic for UploadRemovalListView.xaml
/// </summary>
public partial class UploadRemovalListView : UserControl
{
    public FileGlobCollection GlobCollection { get; }
    public LocalAddon LocalAddon { get; }
    //private ObservableCollection<EditableEntry> Items => GlobCollection.Globs;
    public UploadRemovalListView(FileGlobCollection globCollection, LocalAddon localAddon)
    {
        InitializeComponent();

        GlobCollection = globCollection;
        LocalAddon = localAddon;
        DataContext = GlobCollection;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("The following files will be deleted when you click 'Remove For Upload':\n\n");
        sb.AppendJoin('\n', GlobCollection.GetMatchingFiles(LocalAddon.GamePath));
        DialogHelper.ShowSimplePopup(Window.GetWindow(this), sb.ToString());
    }
}
