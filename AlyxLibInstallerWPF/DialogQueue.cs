using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AlyxLibInstallerWPF;
public static class DialogQueue
{
    //private static readonly ConcurrentQueue<(ContentDialogWpfNew Dialog, Panel Panel)> _queue = new();
    //private static bool _isShowing;

    //public static async Task EnqueueAsync(ContentDialogWpfNew dialog, Panel panel)
    //{
    //    _queue.Enqueue((dialog, panel));
    //    await ProcessNextAsync();
    //}

    //private static async Task ProcessNextAsync()
    //{
    //    if (_isShowing) return;
    //    _isShowing = true;

    //    while (_queue.TryDequeue(out var item))
    //    {
    //        var tcs = new TaskCompletionSource();
    //        item.Dialog.Closed += (_, __) => tcs.TrySetResult();
    //        await item.Dialog.ShowAsync(item.Panel);
    //        await tcs.Task; // wait until the dialog actually closes
    //    }

    //    _isShowing = false;
    //}

    private static readonly SemaphoreSlim _sema = new SemaphoreSlim(1, 1);

    // Shows the dialog on the given panel and returns the clicked result once it closes.
    public static async Task<ContentDialogResult> ShowDialogAsync(ContentDialogWpfNew dialog, Panel panel)
    {
        await _sema.WaitAsync().ConfigureAwait(false);
        try
        {
            // ensure ShowAsync won't throw because it's already showing
            var result = await dialog.ShowAsync(panel);
            return result;
        }
        finally
        {
            _sema.Release();
        }
    }
}