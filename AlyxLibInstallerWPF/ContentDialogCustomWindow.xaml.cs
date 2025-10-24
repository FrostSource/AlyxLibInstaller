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
using System.Windows.Shapes;

namespace AlyxLibInstallerWPF;
/// <summary>
/// Interaction logic for ContentDialogCustomWindow.xaml
/// </summary>
public partial class ContentDialogCustomWindow : Window
{
    public string? PrimaryButtonText { get; set; }
    public string? SecondaryButtonText { get; set; }
    public string? CloseButtonText { get; set; } = "Close";

    public readonly TaskCompletionSource<DialogResultWithData<object>> tcs = new();

    private readonly object? content;

    public ContentDialogCustomWindow()
    {
        InitializeComponent();
        DataContext = this;

        PrimaryButton.Click += Button_Click;
        SecondaryButton.Click += Button_Click;
        CloseButton.Click += Button_Click;
    }

    public ContentDialogCustomWindow(UIElement content) : this()
    {
        Root.Children.Insert(0, content);
        this.content = content;
    }

    public async Task<DialogResultWithData<object>> ShowAsync()
    {
        //ShowDialog();
        await Owner.Dispatcher.BeginInvoke(new Action(() =>
        {
            ShowDialog();
        }));
        return await tcs.Task;
    }

    public void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            DialogResult = !button.IsCancel;
            Close();

            tcs.TrySetResult(new DialogResultWithData<object>
            {
                Result = button.Tag is ContentDialogResult result ? result : ContentDialogResult.None,
                Data = content
            });
        }
    }
}
