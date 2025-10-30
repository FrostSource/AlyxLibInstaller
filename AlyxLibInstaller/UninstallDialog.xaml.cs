using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlyxLibInstaller
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UninstallDialog : Page
    {
        public UninstallDialog()
        {
            this.InitializeComponent();
        }

        public string ExpanderContent
        {
            get => (string)ExpanderControl.Content;
            set => ExpanderControl.Content = value;
        }
    }
}
