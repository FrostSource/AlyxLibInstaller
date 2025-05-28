using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlyxLibInstaller
{
    public sealed partial class AlyxLibInstallOption : UserControl
    {
        public AlyxLibInstallOption()
        {
            this.InitializeComponent();

            CheckBoxControl.Click += CheckBoxControl_Click;
            CheckBoxControl.Checked += CheckBoxControl_Click;
        }

        public event RoutedEventHandler Click;

        private void CheckBoxControl_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        public bool? IsChecked
        {
            get { return CheckBoxControl.IsChecked; }
            set { CheckBoxControl.IsChecked = value; }
        }

        public void ShowInfoBar(InfoBarSeverity severity, string text)
        {
            OptionInfoBar.Severity = severity;
            OptionInfoBar.Message = text;
            OptionInfoBar.IsOpen = true;
        }
        public void HideInfoBar()
        {
            OptionInfoBar.IsOpen = false;
        }

        public string CheckboxContent
        {
            get { return (string)GetValue(CheckboxContentProperty); }
            set { SetValue(CheckboxContentProperty, value); }
        }

        public static readonly DependencyProperty CheckboxContentProperty =
            DependencyProperty.Register("CheckboxContent", typeof(string), typeof(AlyxLibInstallOption), new PropertyMetadata(string.Empty));
        // Dependency property for Tooltip text
        public string TooltipText
        {
            get { return (string)GetValue(TooltipTextProperty); }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    SetValue(TooltipTextProperty, null);
                }
                else
                {
                    SetValue(TooltipTextProperty, value);
                }
            }
        }

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register("TooltipText", typeof(string), typeof(AlyxLibInstallOption), new PropertyMetadata(string.Empty));

        // Dependency property for Description text
        public string DescriptionText
        {
            get { return (string)GetValue(DescriptionTextProperty); }
            set { SetValue(DescriptionTextProperty, value); }
        }

        public static readonly DependencyProperty DescriptionTextProperty =
            DependencyProperty.Register("DescriptionText", typeof(string), typeof(AlyxLibInstallOption), new PropertyMetadata(string.Empty));
    }
}
