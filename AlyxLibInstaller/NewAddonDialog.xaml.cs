using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

using AlyxLibInstaller.AlyxLib;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlyxLibInstaller;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NewAddonDialog : Page
{
    public NewAddonDialog()
    {
        InitializeComponent();
    }

    public string AddonName { get; private set; }

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
