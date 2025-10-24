using System;
using System.Collections.Generic;
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
