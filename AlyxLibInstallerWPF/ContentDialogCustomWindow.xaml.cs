using System.Windows;
using System.Windows.Controls;

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
