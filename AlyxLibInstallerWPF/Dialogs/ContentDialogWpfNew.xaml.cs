using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlyxLibInstallerWPF.Dialogs;
/// <summary>
/// Interaction logic for ContentDialogWpfNew.xaml
/// </summary>
public partial class ContentDialogWpfNew : UserControl
{
    private TaskCompletionSource<ContentDialogResult>? _tcs;

    public event EventHandler? Closed;

    public ContentDialogWpfNew()
    {
        InitializeComponent();

        PrimaryButton.Click += (s, e) => Close(ContentDialogResult.Primary);
        SecondaryButton.Click += (s, e) => Close(ContentDialogResult.Secondary);
        CloseButton.Click += (s, e) => Close(ContentDialogResult.None);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(ContentDialogWpfNew), new PropertyMetadata("Dialog"));

    public object DialogContent
    {
        get => GetValue(DialogContentProperty);
        set => SetValue(DialogContentProperty, value);
    }

    public static readonly DependencyProperty DialogContentProperty =
        DependencyProperty.Register("DialogContent", typeof(object), typeof(ContentDialogWpfNew), new PropertyMetadata(null));

    public async Task<ContentDialogResult> ShowAsync(Panel parent)
    {
        //if (_tcs != null)
        //    throw new InvalidOperationException("Dialog is already showing.");

        //_tcs = new TaskCompletionSource<bool>();

        //// Add dialog to parent
        //parent.Children.Add(this);

        //return _tcs.Task;

        //parent.Children.Add(this);

        //// optional: fade-in animation here
        //await Task.Yield(); // ensure async behavior

        //var tcs = new TaskCompletionSource();
        //void OnClosed(object s, EventArgs e) => tcs.TrySetResult();

        //Closed += OnClosed;
        //await tcs.Task;
        //Closed -= OnClosed;

        if (_tcs != null)
            throw new InvalidOperationException("Dialog is already showing.");

        _tcs = new TaskCompletionSource<ContentDialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Always modify UI on the panel's dispatcher
        if (parent.Dispatcher.CheckAccess())
            parent.Children.Add(this);
        else
            await parent.Dispatcher.InvokeAsync(() => parent.Children.Add(this));

        // Ensure it captures focus and tabs cycle inside
        this.Loaded += OnLoadedFocus;

        return await _tcs.Task;
    }

    private void OnLoadedFocus(object sender, RoutedEventArgs e)
    {
        this.Loaded -= OnLoadedFocus;
        this.Focusable = true;
        this.Focus();
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(this, KeyboardNavigationMode.Cycle);
    }

    private void Close(ContentDialogResult result)
    {

        if (_tcs == null) return;

        var parent = this.Parent as Panel;
        if (parent != null)
        {
            if (parent.Dispatcher.CheckAccess())
                parent.Children.Remove(this);
            else
                parent.Dispatcher.Invoke(() => parent.Children.Remove(this));
        }

        // complete the task
        _tcs.TrySetResult(result);
        _tcs = null;
    }
}
