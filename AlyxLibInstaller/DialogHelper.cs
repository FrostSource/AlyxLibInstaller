using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AlyxLibInstaller;
internal static class DialogHelper
{
    public static async Task<DialogResultWithData<T>> ShowCustomPopupAsync<T>(
        Window window,
        T content,
        string title = "", string primaryButtonText = "", string secondaryButtonText = "", string closeButtonText = "OK")
        where T : FrameworkElement
    {
        if (window.Content is not FrameworkElement rootElement || rootElement is null)
            throw new ArgumentException("Window.Content must be a FrameworkElement with a valid XamlRoot.", nameof(window));

        var dialog = new ContentDialog
        {
            XamlRoot = rootElement.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = title,
            CloseButtonText = closeButtonText,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            Content = content,
            DefaultButton =
                !string.IsNullOrWhiteSpace(primaryButtonText) ? ContentDialogButton.Primary :
                !string.IsNullOrWhiteSpace(secondaryButtonText) ? ContentDialogButton.Secondary :
                !string.IsNullOrWhiteSpace(closeButtonText) ? ContentDialogButton.Close :
                ContentDialogButton.None
        };

        var result = await dialog.ShowAsync();

        return new DialogResultWithData<T>
        {
            Result = result,
            Data = content
        };
    }

    public static async Task<ContentDialogResult> ShowPopupAsync(Window window, string message, string title = "", string primaryButtonText = "", string secondaryButtonText = "", string closeButtonText = "OK")
    {
        var result = await ShowCustomPopupAsync(window, new SimpleTextDialog(message), title, primaryButtonText, secondaryButtonText, closeButtonText);
        return result.Result;
    }

    /// <summary>
    /// Show a simple popup with a message and an OK button.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="title"></param>
    public static async void ShowSimplePopup(Window window, string message, string title = "")
    {
        await ShowPopupAsync(window, message, title, "OK", "", "");
    }

    public static void ShowWarningPopup(Window window, string message)
    {
        ShowSimplePopup(window, message, "Warning");
    }

    public static async Task<StorageFolder?> FolderPickerAsync(Window window, IEnumerable<string> filters)
    {
        // Create a folder picker
        var openPicker = new FolderPicker();

        // See the sample code below for how to make the window accessible from the App class.
        //var window = App.MainWindow;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the folder picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

        foreach ( var filter in filters ) openPicker.FileTypeFilter.Add(filter);

        // Open the picker for the user to pick a folder
        return await openPicker.PickSingleFolderAsync();
    }

    public static async Task<StorageFolder?> FolderPickerAsync(Window window)
    {
        return await FolderPickerAsync(window, ["*"]);
    }
}
