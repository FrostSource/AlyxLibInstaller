using System.Windows;
using System.Windows.Input;

namespace AlyxLibInstallerWPF
{
    public partial class ContentDialogWindow : Window
    {
        private TaskCompletionSource<ContentDialogResult> _tcs;
        public ContentDialogResult Result { get; private set; } = ContentDialogResult.None;

        private Window? _ownerWindow;
        private bool _ownerWasEnabled = true;

        public ContentDialogWindow()
        {
            InitializeComponent();
            Loaded += ContentDialogWindow_Loaded;
            Closed += ContentDialogWindow_Closed;
            PreviewKeyDown += ContentDialogWindow_PreviewKeyDown;
        }

        private void ContentDialogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var model = DataContext as ContentDialog;

            SecondaryButton.Visibility = string.IsNullOrEmpty(model?.SecondaryButtonText) ? Visibility.Collapsed : Visibility.Visible;
            CloseButton.Visibility = string.IsNullOrEmpty(model?.CloseButtonText) ? Visibility.Collapsed : Visibility.Visible;
            PrimaryButton.Visibility = string.IsNullOrEmpty(model?.PrimaryButtonText) ? Visibility.Collapsed : Visibility.Visible;

            var defaultBtn = model?.DefaultButton ?? ContentDialogButton.Primary;
            switch (defaultBtn)
            {
                case ContentDialogButton.Primary:
                    PrimaryButton.Focus();
                    break;
                case ContentDialogButton.Secondary:
                    SecondaryButton.Focus();
                    break;
                case ContentDialogButton.Close:
                    CloseButton.Focus();
                    break;
            }

            // Modal behavior: disable owner so user cannot interact with it.
            if (Owner is Window ow)
            {
                _ownerWindow = ow;
                _ownerWasEnabled = ow.IsEnabled;
                try
                {
                    ow.IsEnabled = false;
                }
                catch
                {
                    // best-effort; avoid throwing if owner blocks changes for some reason
                }
            }
        }

        private void OwnerWindow_OnLocationOrSizeChanged(object? s, EventArgs e)
        {
            // Ensure UI thread update
            Dispatcher?.BeginInvoke(new Action(UpdateBoundsToOwner));
        }

        private void OwnerWindow_OnStateChanged(object? s, EventArgs e)
        {
            Dispatcher?.BeginInvoke(new Action(UpdateBoundsToOwner));
        }

        private void OwnerWindow_OnClosed(object? s, EventArgs e)
        {
            // If owner is closed, close dialog as well
            Dispatcher?.BeginInvoke(new Action(() =>
            {
                try { Close(); } catch { }
            }));
        }

        private void UpdateBoundsToOwner()
        {
            if (_ownerWindow is Window ow)
            {
                // If the owner was moved to another monitor or DPI changed, these values
                // keep the dialog overlay covering the owner area.
                Left = ow.Left;
                Top = ow.Top;
                Width = ow.ActualWidth > 0 ? ow.ActualWidth : ow.Width;
                Height = ow.ActualHeight > 0 ? ow.ActualHeight : ow.Height;
            }
        }

        private void ContentDialogWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Result = ContentDialogResult.None;
                Close();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                var model = DataContext as ContentDialog;
                var defaultBtn = model?.DefaultButton ?? ContentDialogButton.Primary;
                if (defaultBtn == ContentDialogButton.Primary && PrimaryButton.IsVisible)
                {
                    PrimaryButton_Click(this, null);
                    e.Handled = true;
                }
                else if (defaultBtn == ContentDialogButton.Secondary && SecondaryButton.IsVisible)
                {
                    SecondaryButton_Click(this, null);
                    e.Handled = true;
                }
                else if (defaultBtn == ContentDialogButton.Close && CloseButton.IsVisible)
                {
                    CloseButton_Click(this, null);
                    e.Handled = true;
                }
            }
        }

        public Task<ContentDialogResult> ShowAsTaskAsync()
        {
            _tcs = new TaskCompletionSource<ContentDialogResult>();
            Show();
            return _tcs.Task;
        }

        private void ContentDialogWindow_Closed(object sender, EventArgs e)
        {
            // Restore owner (best-effort)
            if (_ownerWindow != null)
            {
                try
                {
                    _ownerWindow.IsEnabled = _ownerWasEnabled;
                    // try to reactivate owner so keyboard/activation returns sensibly
                    _ownerWindow.Activate();
                }
                catch
                {
                    // swallow — we must not throw during cleanup
                }
                _ownerWindow = null;
            }

            _tcs?.TrySetResult(Result);
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ContentDialogResult.Primary;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ContentDialogResult.Secondary;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ContentDialogResult.None;
            Close();
        }
    }
}
