using AlyxLibInstallerShared.ViewModels;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AlyxLibInstallerWPF;

public partial class MainWindow : Window
{
    private const double InstallPulseScale = 1.25;
    private static readonly TimeSpan InstallPulseDuration = TimeSpan.FromMilliseconds(180);

    private const uint FLASHW_STOP = 0;
    private const uint FLASHW_TRAY = 0x00000002;

    private AlyxLibInstallerSharedViewModel? _viewModel;
    private bool _reinstallReminderPending;
    private bool _pulseOnNextActivation;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => MainWindow_Activated_FirstTime();
        Activated += MainWindow_Activated;
        Deactivated += MainWindow_Deactivated;
        DataContextChanged += MainWindow_DataContextChanged;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Activated_FirstTime()
    {
        // WPF console starts with paragraphs by default so clear it
        ///TODO Make sure this plays nice with viewmodel, move to viewmodel message/event
        DebugConsole.Document.Blocks.Clear();

        AttachViewModel(DataContext as AlyxLibInstallerSharedViewModel);
    }

    private void UninstallSplitButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // prevents bubbling to main button

        if (((Button)sender).ContextMenu != null)
        {
            ((Button)sender).ContextMenu.PlacementTarget = (Button)sender;
            ((Button)sender).ContextMenu.IsOpen = true;
        }
    }

    private void ClearDebugConsole_Click(object sender, RoutedEventArgs e)
    {
        DebugConsole.Document.Blocks.Clear();
    }

    private void MenuBarExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        AttachViewModel(e.NewValue as AlyxLibInstallerSharedViewModel);
    }

    private void MainWindow_Activated(object? sender, EventArgs e)
    {
        if (!_reinstallReminderPending)
        {
            return;
        }

        StopTaskbarFlash();

        if (_pulseOnNextActivation)
        {
            PulseInstallButton();
            _pulseOnNextActivation = false;
        }
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        if (!_reinstallReminderPending)
        {
            return;
        }

        FlashTaskbarOnce();
        _pulseOnNextActivation = true;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        StopTaskbarFlash();

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }

    private void AttachViewModel(AlyxLibInstallerSharedViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _reinstallReminderPending = _viewModel.AlyxLibRemovedFromAddonForUpload;
        }
        else
        {
            _reinstallReminderPending = false;
        }

        _pulseOnNextActivation = false;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AlyxLibInstallerSharedViewModel.AlyxLibRemovedFromAddonForUpload)
            || _viewModel == null)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ViewModel_PropertyChanged(sender, e));
            return;
        }

        var isPending = _viewModel.AlyxLibRemovedFromAddonForUpload;
        if (isPending == _reinstallReminderPending)
        {
            return;
        }

        _reinstallReminderPending = isPending;

        if (isPending)
        {
            _pulseOnNextActivation = false;

            if (IsActive)
            {
                PulseInstallButton();
            }
            else
            {
                _pulseOnNextActivation = true;
            }
        }
        else
        {
            _pulseOnNextActivation = false;
            StopInstallPulse();
            StopTaskbarFlash();
        }
    }

    private void PulseInstallButton()
    {
        if (InstallButton.RenderTransform is not ScaleTransform scaleTransform)
        {
            scaleTransform = new ScaleTransform(1, 1);
            InstallButton.RenderTransform = scaleTransform;
        }

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        scaleTransform.ScaleX = 1;
        scaleTransform.ScaleY = 1;

        var scaleAnimationX = CreateInstallPulseAnimation();
        var scaleAnimationY = CreateInstallPulseAnimation();

        scaleAnimationX.Completed += (_, _) =>
        {
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scaleTransform.ScaleX = 1;
            scaleTransform.ScaleY = 1;
        };

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimationX);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimationY);
    }

    private void StopInstallPulse()
    {
        if (InstallButton.RenderTransform is not ScaleTransform scaleTransform)
        {
            return;
        }

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        scaleTransform.ScaleX = 1;
        scaleTransform.ScaleY = 1;
    }

    private static DoubleAnimation CreateInstallPulseAnimation()
    {
        return new DoubleAnimation
        {
            From = 1,
            To = InstallPulseScale,
            Duration = InstallPulseDuration,
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2),
            FillBehavior = FillBehavior.Stop
        };
    }

    private void FlashTaskbarOnce()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var flashInfo = new FLASHWINFO
        {
            cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
            hwnd = windowHandle,
            dwFlags = FLASHW_TRAY,
            uCount = 2,
            dwTimeout = 0
        };

        FlashWindowEx(ref flashInfo);
    }

    private void StopTaskbarFlash()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var flashInfo = new FLASHWINFO
        {
            cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
            hwnd = windowHandle,
            dwFlags = FLASHW_STOP,
            uCount = 0,
            dwTimeout = 0
        };

        FlashWindowEx(ref flashInfo);
    }

    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumericTextBoxRegex();

    private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        e.Handled = NumericTextBoxRegex().IsMatch(e.Text);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FLASHWINFO flashInfo);
}
