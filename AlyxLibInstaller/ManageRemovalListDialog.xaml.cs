using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlyxLibInstaller;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ManageRemovalListDialog : Page
{
    public ObservableCollection<string> TestSource { get; set; } = [
        "Test1", "Test2", "Test3"
        ];
    public ManageRemovalListDialog()
    {
        InitializeComponent();

        FileRemovalListView.ItemsSource = TestSource;
    }
}
