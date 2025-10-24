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

namespace AlyxLibInstallerWPF
{
    /// <summary>
    /// Interaction logic for InfoBar.xaml
    /// </summary>
    public partial class InfoBarWPF : UserControl
    {
        public InfoBarWPF()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(InfoBarWPF), new PropertyMetadata(string.Empty));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty SeverityProperty =
            DependencyProperty.Register(nameof(Severity), typeof(string), typeof(InfoBarWPF),
                new PropertyMetadata("Informational", OnSeverityChanged));

        public string Severity
        {
            get => (string)GetValue(SeverityProperty);
            set => SetValue(SeverityProperty, value);
        }

        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(InfoBarWPF), new PropertyMetadata(true));

        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(InfoBarWPF), new PropertyMetadata(false));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public string Icon { get; private set; } = "ℹ"; // default info
        public Brush Background { get; private set; } = Brushes.LightBlue;

        private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InfoBarWPF bar)
            {
                switch (e.NewValue as string)
                {
                    case "Error":
                        bar.Icon = "❌";
                        bar.Background = Brushes.MistyRose;
                        break;
                    case "Warning":
                        bar.Icon = "⚠";
                        bar.Background = Brushes.LemonChiffon;
                        break;
                    case "Success":
                        bar.Icon = "✔";
                        bar.Background = Brushes.Honeydew;
                        break;
                    default:
                        bar.Icon = "ℹ";
                        bar.Background = Brushes.LightBlue;
                        break;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsOpen = false;
        }
    }
}
