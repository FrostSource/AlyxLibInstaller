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
