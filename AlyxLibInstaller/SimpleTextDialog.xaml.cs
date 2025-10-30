using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlyxLibInstaller
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SimpleTextDialog : Page
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
}
