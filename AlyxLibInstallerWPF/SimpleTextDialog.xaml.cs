using System.Windows.Controls;

namespace AlyxLibInstallerWPF;
/// <summary>
/// Interaction logic for SimpleTextDialog.xaml
/// </summary>
public partial class SimpleTextDialog : Page
{
    public SimpleTextDialog()
    {
        this.InitializeComponent();
    }
    public SimpleTextDialog(string text) : this()
    {
        DialogText = text;
    }

    public string DialogText
    {
        get
        {
            return DialogTextBlock.Text;
        }
        set
        {
            DialogTextBlock.Text = value;
        }
    }
}
