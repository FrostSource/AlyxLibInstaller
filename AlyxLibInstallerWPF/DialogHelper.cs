using Microsoft.Win32; // for File/Folder dialogs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace AlyxLibInstallerWPF;

internal static class DialogHelper
{
    public static async Task<DialogResultWithData<T>> ShowCustomPopupAsync<T>(
        Window owner,
        T content,
        string title = "",
        string primaryButtonText = "",
        string secondaryButtonText = "",
        string closeButtonText = "OK")
        where T : UIElement
    {
        var tcs = new TaskCompletionSource<DialogResultWithData<T>>();

        UIElement contentElement = content is Page page ? new Frame { Content = page, NavigationUIVisibility = NavigationUIVisibility.Hidden } : content;





        //var buttonPanel = new StackPanel
        //{
        //    Orientation = Orientation.Horizontal,
        //    HorizontalAlignment = HorizontalAlignment.Stretch, // center buttons
        //    Margin = new Thickness(0, 0, 0, 10),
        //    Background = (System.Windows.Media.Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"],
        //};

        //if (!string.IsNullOrEmpty(primaryButtonText))
        //    buttonPanel.Children.Add(new Button { Tag = ContentDialogResult.Primary, Content = primaryButtonText, IsDefault = true, Margin = new Thickness(4, 0, 0, 0) });

        //if (!string.IsNullOrEmpty(secondaryButtonText))
        //    buttonPanel.Children.Add(new Button { Tag = ContentDialogResult.Secondary, Content = secondaryButtonText, Margin = new Thickness(4, 0, 0, 0) });

        //if (!string.IsNullOrEmpty(closeButtonText))
        //    buttonPanel.Children.Add(new Button { Tag = ContentDialogResult.None, Content = closeButtonText, IsCancel = true, Margin = new Thickness(4, 0, 0, 0) });


        //var dialogWindow = new Window
        //{
        //    Title = title,
        //    Content = new ContentDialogCustom(contentElement)
        //    {
        //        PrimaryButtonText = primaryButtonText,
        //        SecondaryButtonText = secondaryButtonText,
        //        CloseButtonText = closeButtonText
        //    },
        //    //Content = new StackPanel
        //    //{
        //    //    Children =
        //    //    {
        //    //        contentElement,
        //    //        buttonPanel,
        //    //        //new StackPanel
        //    //        //{
        //    //        //    Orientation = Orientation.Horizontal,
        //    //        //    HorizontalAlignment = HorizontalAlignment.Center,
        //    //        //    Margin = new Thickness(0,10,0,0),
        //    //        //    Children =
        //    //        //    {
        //    //        //        !string.IsNullOrEmpty(primaryButtonText) ?
        //    //        //            new Button { Tag=ContentDialogResult.Primary, Content = primaryButtonText, IsDefault = true, Margin = new Thickness(4,0,0,0) } : null,
        //    //        //        !string.IsNullOrEmpty(secondaryButtonText) ?
        //    //        //            new Button { Tag=ContentDialogResult.Secondary, Content = secondaryButtonText, Margin = new Thickness(4,0,0,0) } : null,
        //    //        //        !string.IsNullOrEmpty(closeButtonText) ?
        //    //        //            new Button { Tag=ContentDialogResult.None, Content = closeButtonText, IsCancel = true, Margin = new Thickness(4,0,0,0) } : null
        //    //        //    }
        //    //        //}
        //    //    }
        //    //},
        //    SizeToContent = SizeToContent.WidthAndHeight,
        //    WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //    Owner = owner
        //};

#pragma warning disable WPF0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var dialogWindow = new ContentDialogCustomWindow(contentElement)
        {
            Title = title,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            CloseButtonText = closeButtonText,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,

            WindowStyle = WindowStyle.None,
            Background = Brushes.Transparent,
            AllowsTransparency = true
            //ThemeMode = owner.ThemeMode
        };
#pragma warning restore WPF0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        //// Button click handling
        //foreach (var btn in ((StackPanel)((StackPanel)dialogWindow.Content).Children[1]).Children)
        //{
        //    if (btn is Button button)
        //    {
        //        button.Click += (s, e) =>
        //        {
        //            dialogWindow.DialogResult = button.IsCancel ? false : true;
        //            dialogWindow.Close();

        //            tcs.TrySetResult(new DialogResultWithData<T>
        //            {
        //                Result = button.Tag is ContentDialogResult result ? result : ContentDialogResult.None,
        //                Data = content
        //            });
        //        };
        //    }
        //}

        dialogWindow.Focus();
        dialogWindow.ShowDialog();
        //await dialogWindow.ShowAsync();

        var result = await dialogWindow.tcs.Task;
        return new DialogResultWithData<T>
        {
            Result = result.Result,
            Data = content
        };

        //dialogWindow.ShowDialog();
        //owner.Dispatcher.BeginInvoke(new Action(() =>
        //{
        //    dialogWindow.ShowDialog();
        //}));
        //return tcs.Task;
    }

    public static async Task<ContentDialogResult> ShowPopupAsync(Window owner, string message, string title = "", string primaryButtonText = "OK", string secondaryButtonText = "", string closeButtonText = "")
    {
        //TODO: Make custom ui for this
        var textBlock = new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 10) };
        var result = await ShowCustomPopupAsync(owner, textBlock, title, primaryButtonText, secondaryButtonText, closeButtonText);
        return result.Result;
    }

    /// <summary>
    /// Show a simple popup with a message and an OK button.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="title"></param>
    public static void ShowSimplePopup(Window owner, string message, string title = "Info")
    {
        System.Windows.MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static void ShowWarningPopup(Window owner, string message)
    {
        System.Windows.MessageBox.Show(owner, message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public static string? FolderPicker(Window? owner = null, string initialDirectory = "")
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder",
            InitialDirectory = initialDirectory
        };

        bool? result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        return result == true ? dialog.FolderName : null;
    }
}
