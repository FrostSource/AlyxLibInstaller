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
/// Interaction logic for ContentDialogCustom.xaml
/// </summary>
public partial class ContentDialogCustom : Page
{
    public string? PrimaryButtonText { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? CloseButtonText { get; set; } = "Close";

    public ContentDialogCustom()
    {
        InitializeComponent();
        DataContext = this;
    }

    public ContentDialogCustom(UIElement content) : this()
    {
        Root.Children.Insert(0, content);
    }
}
