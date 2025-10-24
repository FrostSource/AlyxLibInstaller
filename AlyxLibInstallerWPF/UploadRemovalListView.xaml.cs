using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using AlyxLibInstallerShared.Models;
using Source2HelperLibrary;

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
