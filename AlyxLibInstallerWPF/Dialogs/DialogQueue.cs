using System.Windows.Controls;

namespace AlyxLibInstallerWPF.Dialogs;
public static class DialogQueue
{
    //private static readonly SemaphoreSlim _sema = new SemaphoreSlim(1, 1);
    //private static readonly ConcurrentDictionary<Panel, SemaphoreSlim> _locks = new();
    //private static SemaphoreSlim GetLock(Panel panel) => _locks.GetOrAdd(panel, _ => new SemaphoreSlim(1, 1));

    // Shows the dialog on the given panel and returns the clicked result once it closes.
    public static async Task<ContentDialogResult> ShowDialogAsync(ContentDialogWpfNew dialog, Panel panel)
    {
        //NOTE: Locking was removed to allow stacking popups, e.g. FileRemovalList>View Files
        //      As long as subsequent dialogs on the same panel are awaited they shouldn't overlap

        //await GetLock(panel).WaitAsync().ConfigureAwait(false);
        //await _sema.WaitAsync().ConfigureAwait(false);
        try
        {
            // ensure ShowAsync won't throw because it's already showing
            var result = await dialog.ShowAsync(panel);
            return result;
        }
        finally
        {
            //_sema.Release();
            //GetLock(panel).Release();
        }
    }
}