using System.Windows.Controls;

namespace AlyxLibInstallerWPF;
/// <summary>
/// Interaction logic for ListView.xaml
/// </summary>
public partial class StringListView : UserControl
{
    public IEnumerable<string> StringList { get; set; } = ["Test 1", "Test 2", "Test 3", "Test 4", "Test 5", "Test 6", "Test 7", "Test 8", "Test 9", "Test 10"];
    public string? Message { get; set; } = "";
    public StringListView()
    {
        StringList = [];
        InitializeComponent();
        DataContext = this;
    }

    public StringListView(IList<string> stringList) : this()
    {
        StringList = stringList;
    }

    public StringListView(IList<string> stringList, string message) : this(stringList)
    {
        Message = message;
    }
}
