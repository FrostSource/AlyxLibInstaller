using AlyxLib;
using System.Windows.Controls;

namespace AlyxLibInstallerWPF;
/// <summary>
/// Interaction logic for NewAddonDialog.xaml
/// </summary>
public partial class NewAddonDialog : Page
{
    public NewAddonDialog()
    {
        InitializeComponent();
    }

    public string AddonName { get; private set; } = "";

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox != null)
        {
            //string input = textBox.Text;

            //string filtered = new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            //if (input != filtered)
            //{
            //    textBox.Text = filtered;

            //    textBox.SelectionStart = filtered.Length;
            //}

            AddonName = textBox.Text;

            if (!AlyxLibHelpers.StringIsValidModName(textBox.Text))
            {
                //AddonNameWarningBar.Message = "Invalid name! Must contain only letters, numbers and underscore characters.";
                //AddonModNameErrorText.Visibility = Visibility.Visible;
                AddonNameWarningBar.Opacity = 1;
            }
            else
            {
                //AddonNameWarningBar.Message = "";
                //AddonModNameErrorText.Visibility = Visibility.Collapsed;
                AddonNameWarningBar.Opacity = 0;
            }
        }
    }
}
