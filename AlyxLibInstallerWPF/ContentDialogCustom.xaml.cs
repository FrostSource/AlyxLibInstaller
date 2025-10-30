using System.Windows;
using System.Windows.Controls;

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
