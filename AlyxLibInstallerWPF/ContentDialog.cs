// File: ContentDialog.cs
using System.Windows;

namespace AlyxLibInstallerWPF
{
    public enum ContentDialogButton
    {
        Primary,
        Secondary,
        Close
    }

    //public enum ContentDialogResult
    //{
    //    Primary,
    //    Secondary,
    //    None
    //}

    public class ContentDialog
    {
        public string Title { get; set; }
        public object Content { get; set; }
        public string PrimaryButtonText { get; set; }
        public string SecondaryButtonText { get; set; }
        public string CloseButtonText { get; set; }
        public ContentDialogButton DefaultButton { get; set; } = ContentDialogButton.Primary;
        public Style Style { get; set; } // optionally pass a Window style (e.g. DefaultContentDialogStyle)

        /// <summary>
        /// Shows the dialog and returns which button the user pressed.
        /// </summary>
        // ContentDialog.cs (or wherever ShowAsync lives)
        public Task<ContentDialogResult> ShowAsync()
        {
            var dlg = new ContentDialogWindow
            {
                DataContext = this
            };

            var owner = Application.Current?.MainWindow;
            if (owner != null)
            {
                dlg.Owner = owner;
                dlg.WindowStartupLocation = WindowStartupLocation.Manual;

                // Match the owner bounds so the overlay covers only the app window
                dlg.Left = owner.Left;
                dlg.Top = owner.Top;
                dlg.Width = owner.ActualWidth > 0 ? owner.ActualWidth : owner.Width;
                dlg.Height = owner.ActualHeight > 0 ? owner.ActualHeight : owner.Height;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (Style != null) dlg.Style = Style;

            // Show non-blocking and return Task — the window's Loaded handler will disable the owner.
            return dlg.ShowAsTaskAsync();
        }

    }
}
