using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services.Dialog;
using Source2HelperLibrary;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AlyxLibInstallerWPF.Dialogs;

/// <summary>
/// Interaction logic for UploadRemovalListView.xaml
/// </summary>
public partial class UploadRemovalListView : UserControl
{
    private const double ToggleLeftMargin = 2;
    private const double ToggleRightMargin = 26;

    private static readonly DependencyProperty ToggleAnimationReadyProperty =
        DependencyProperty.RegisterAttached(
            "ToggleAnimationReady",
            typeof(bool),
            typeof(UploadRemovalListView),
            new PropertyMetadata(false));

    public FileGlobCollection GlobCollection { get; }
    public LocalAddon LocalAddon { get; }
    public IDialogService DialogService { get; }

    public UploadRemovalListView(FileGlobCollection globCollection, LocalAddon localAddon, IDialogService dialogService)
    {
        InitializeComponent();

        GlobCollection = globCollection;
        LocalAddon = localAddon;
        DialogService = dialogService;
        DataContext = GlobCollection;
    }

    private static bool GetToggleAnimationReady(DependencyObject element)
    {
        return (bool)element.GetValue(ToggleAnimationReadyProperty);
    }

    private static void SetToggleAnimationReady(DependencyObject element, bool value)
    {
        element.SetValue(ToggleAnimationReadyProperty, value);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var files = GlobCollection.GetMatchingFiles(LocalAddon.GamePath);

        if (GlobCollection.Count == 0 || files.Length == 0)
        {
            DialogService.ShowTextPopup(new DialogConfiguration
            {
                Title = "File Removal List",
                Message = GlobCollection.Count == 0
                    ? "No file patterns have been added to the removal list. No files will be removed."
                    : "No files matched by patterns in the removal list. No files will be removed.",
                CancelButtonText = "Close"
            });
        }
        else
        {
            DialogService.ShowListPopup(new DialogConfiguration
            {
                Title = "File Removal List",
                Message = "These files match your upload removal rules. Temporary files are restored on the next Install.",
                CancelButtonText = "Close"
            }, files);
        }
    }

    private void ModeToggle_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle)
        {
            return;
        }

        ApplyToggleThumbPosition(toggle, animate: false);

        toggle.Dispatcher.BeginInvoke(new Action(() =>
        {
            SetToggleAnimationReady(toggle, true);
        }), DispatcherPriority.Loaded);
    }

    private void ModeToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle)
        {
            ApplyToggleThumbPosition(toggle, animate: GetToggleAnimationReady(toggle));
        }
    }

    private void ModeToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle)
        {
            ApplyToggleThumbPosition(toggle, animate: GetToggleAnimationReady(toggle));
        }
    }

    private static void ApplyToggleThumbPosition(ToggleButton toggle, bool animate)
    {
        if (toggle.Template.FindName("Thumb", toggle) is not Border thumb)
        {
            return;
        }

        var targetMargin = new Thickness(toggle.IsChecked == true ? ToggleRightMargin : ToggleLeftMargin, 0, 0, 0);

        if (!animate)
        {
            thumb.BeginAnimation(MarginProperty, null);
            thumb.Margin = targetMargin;
            return;
        }

        var currentMargin = thumb.Margin;

        thumb.BeginAnimation(MarginProperty, null);
        thumb.Margin = currentMargin;

        var animation = new ThicknessAnimation
        {
            From = currentMargin,
            To = targetMargin,
            Duration = TimeSpan.FromMilliseconds(toggle.IsChecked == true ? 160 : 140),
            AccelerationRatio = toggle.IsChecked == true ? 0 : 0.25,
            DecelerationRatio = 0.75,
            FillBehavior = FillBehavior.Stop
        };

        animation.Completed += (_, _) =>
        {
            thumb.BeginAnimation(MarginProperty, null);
            thumb.Margin = targetMargin;
        };

        thumb.BeginAnimation(MarginProperty, animation);
    }
}
