using Steamworks;

namespace AlyxLib.Workshop;

/// <summary>
/// Talks to the Steam Workshop through the running Steam client, signed in as the user.
/// Steam shows Half-Life: Alyx as running while a client is open, so it should only be kept open for one task.
/// </summary>
public sealed class SteamWorkshopClient : IWorkshopClient
{
    public const uint AppId = 546560;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
    private static int _openClients;

    private bool _disposed;

    private SteamWorkshopClient() { }

    /// <summary>
    /// Connects to Steam as Half-Life: Alyx.
    /// </summary>
    /// <exception cref="WorkshopException">Steam isn't running or the connection failed.</exception>
    public static SteamWorkshopClient Connect()
    {
        // Steam only allows one connection per process
        if (Interlocked.Exchange(ref _openClients, 1) == 1)
            throw new InvalidOperationException("A Steam Workshop connection is already open.");

        try
        {
            // Tells Steam which game is connecting, instead of needing steam_appid.txt next to the exe
            Environment.SetEnvironmentVariable("SteamAppId", AppId.ToString());
            Environment.SetEnvironmentVariable("SteamGameId", AppId.ToString());

            var result = SteamAPI.InitEx(out string error);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                throw new WorkshopException(result switch
                {
                    ESteamAPIInitResult.k_ESteamAPIInitResult_NoSteamClient => "Couldn't connect to Steam. Make sure Steam is running and you're signed in, then try again.",
                    ESteamAPIInitResult.k_ESteamAPIInitResult_VersionMismatch => "Couldn't connect to Steam because it needs updating. Restart Steam to update it, then try again.",
                    _ => $"Couldn't connect to Steam: {error}"
                });
            }

            return new SteamWorkshopClient();
        }
        catch (DllNotFoundException)
        {
            Interlocked.Exchange(ref _openClients, 0);
            throw new WorkshopException("steam_api64.dll is missing, so Steam can't be reached. Try downloading the installer again.");
        }
        catch
        {
            Interlocked.Exchange(ref _openClients, 0);
            throw;
        }
    }

    public async Task<WorkshopItemDetails?> GetItemAsync(ulong itemId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var query = SteamUGC.CreateQueryUGCDetailsRequest([new PublishedFileId_t(itemId)], 1);
        if (query == UGCQueryHandle_t.Invalid)
            throw new WorkshopException("Steam couldn't start looking up the Workshop item.");

        try
        {
            SteamUGC.SetReturnLongDescription(query, true);
            SteamUGC.SetReturnChildren(query, true);
            // Cached results can be missing edits made on the Workshop page
            SteamUGC.SetAllowCachedResponse(query, 0);

            var completed = await WaitAsync<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(query), cancellationToken);
            if (completed.m_eResult != EResult.k_EResultOK)
                throw new WorkshopException($"Steam couldn't look up the Workshop item: {Describe(completed.m_eResult)}");

            if (completed.m_unNumResultsReturned == 0
                || !SteamUGC.GetQueryUGCResult(query, 0, out var details)
                || details.m_eResult != EResult.k_EResultOK)
                return null;

            if (details.m_ulSteamIDOwner != SteamUser.GetSteamID().m_SteamID)
                throw new WorkshopException($"Workshop item {itemId} belongs to another Steam account, so it can't be updated.");

            SteamUGC.GetQueryUGCPreviewURL(query, 0, out string previewUrl, 1024);

            var children = new PublishedFileId_t[details.m_unNumChildren];
            if (children.Length > 0)
                SteamUGC.GetQueryUGCChildren(query, 0, children, (uint)children.Length);

            return new WorkshopItemDetails(
                itemId,
                details.m_rgchTitle,
                details.m_rgchDescription,
                (WorkshopVisibility)details.m_eVisibility,
                details.m_rgchTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                [.. children.Select(child => child.m_PublishedFileId)],
                previewUrl,
                DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeUpdated));
        }
        finally
        {
            SteamUGC.ReleaseQueryUGCRequest(query);
        }
    }

    public async Task<WorkshopCreateResult> CreateItemAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var created = await WaitAsync<CreateItemResult_t>(SteamUGC.CreateItem(new AppId_t(AppId), EWorkshopFileType.k_EWorkshopFileTypeCommunity), cancellationToken);
        if (created.m_eResult != EResult.k_EResultOK)
            throw new WorkshopException($"Steam couldn't create the Workshop item: {Describe(created.m_eResult)}");

        return new WorkshopCreateResult(created.m_nPublishedFileId.m_PublishedFileId, created.m_bUserNeedsToAcceptWorkshopLegalAgreement);
    }

    public async Task<WorkshopSubmitResult> SubmitUpdateAsync(WorkshopItemUpdate update, IProgress<WorkshopUploadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var itemId = new PublishedFileId_t(update.ItemId);
        var handle = SteamUGC.StartItemUpdate(new AppId_t(AppId), itemId);

        void Set(bool accepted, string field)
        {
            if (!accepted)
                throw new WorkshopException($"Steam didn't accept the item's {field}.");
        }

        if (update.Title != null) Set(SteamUGC.SetItemTitle(handle, update.Title), "title");
        if (update.Description != null) Set(SteamUGC.SetItemDescription(handle, update.Description), "description");
        if (update.Visibility != null) Set(SteamUGC.SetItemVisibility(handle, (ERemoteStoragePublishedFileVisibility)update.Visibility), "visibility");
        if (update.Tags != null) Set(SteamUGC.SetItemTags(handle, [.. update.Tags]), "tags");
        if (update.ContentFolder != null) Set(SteamUGC.SetItemContent(handle, Path.GetFullPath(update.ContentFolder)), "content folder");
        if (update.PreviewFile != null) Set(SteamUGC.SetItemPreview(handle, Path.GetFullPath(update.PreviewFile)), "preview image");

        var submitted = await WaitAsync<SubmitItemUpdateResult_t>(SteamUGC.SubmitItemUpdate(handle, update.ChangeNote), cancellationToken, () =>
        {
            var status = SteamUGC.GetItemUpdateProgress(handle, out ulong processed, out ulong total);
            var stage = status switch
            {
                EItemUpdateStatus.k_EItemUpdateStatusPreparingConfig => "Preparing",
                EItemUpdateStatus.k_EItemUpdateStatusPreparingContent => "Preparing content",
                EItemUpdateStatus.k_EItemUpdateStatusUploadingContent => "Uploading content",
                EItemUpdateStatus.k_EItemUpdateStatusUploadingPreviewFile => "Uploading preview image",
                EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges => "Saving changes",
                _ => null
            };
            if (stage != null)
                progress?.Report(new WorkshopUploadProgress(stage, total > 0 ? (double)processed / total : null));
        });

        if (submitted.m_eResult != EResult.k_EResultOK)
            throw new WorkshopException($"Steam rejected the update: {Describe(submitted.m_eResult)}");

        if (update.RequiredItems != null)
            await SetRequiredItemsAsync(update.ItemId, update.RequiredItems, cancellationToken);

        return new WorkshopSubmitResult(submitted.m_bUserNeedsToAcceptWorkshopLegalAgreement);
    }

    /// <summary>
    /// Adds and removes required items so they match <paramref name="requiredItems"/>.
    /// Steam keeps these separately from the item update.
    /// </summary>
    private async Task SetRequiredItemsAsync(ulong itemId, IReadOnlyList<ulong> requiredItems, CancellationToken cancellationToken)
    {
        var current = (await GetItemAsync(itemId, cancellationToken))?.RequiredItems ?? [];
        var parent = new PublishedFileId_t(itemId);

        foreach (var added in requiredItems.Except(current))
        {
            var result = await WaitAsync<AddUGCDependencyResult_t>(SteamUGC.AddDependency(parent, new PublishedFileId_t(added)), cancellationToken);
            if (result.m_eResult != EResult.k_EResultOK)
                throw new WorkshopException($"Steam couldn't add required item {added}: {Describe(result.m_eResult)}");
        }

        foreach (var removed in current.Except(requiredItems))
        {
            var result = await WaitAsync<RemoveUGCDependencyResult_t>(SteamUGC.RemoveDependency(parent, new PublishedFileId_t(removed)), cancellationToken);
            if (result.m_eResult != EResult.k_EResultOK)
                throw new WorkshopException($"Steam couldn't remove required item {removed}: {Describe(result.m_eResult)}");
        }
    }

    /// <summary>
    /// Waits for a Steam API call to finish, running Steam's callbacks until it does.
    /// </summary>
    /// <param name="onPoll">Called each time callbacks are run, e.g. to report progress.</param>
    private static async Task<T> WaitAsync<T>(SteamAPICall_t call, CancellationToken cancellationToken, Action? onPoll = null) where T : struct
    {
        if (call == SteamAPICall_t.Invalid)
            throw new WorkshopException("Steam couldn't start the request.");

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callResult = CallResult<T>.Create((result, ioFailure) =>
        {
            if (ioFailure)
                completion.TrySetException(new WorkshopException("Lost connection to Steam."));
            else
                completion.TrySetResult(result);
        });
        callResult.Set(call);

        while (!completion.Task.IsCompleted)
        {
            SteamAPI.RunCallbacks();
            onPoll?.Invoke();
            await Task.Delay(PollInterval, cancellationToken);
        }

        return await completion.Task;
    }

    private static string Describe(EResult result) => result switch
    {
        EResult.k_EResultFileNotFound => "the item or a file to upload couldn't be found.",
        EResult.k_EResultAccessDenied => "access denied. You may need to accept the Steam Workshop legal agreement.",
        EResult.k_EResultInsufficientPrivilege or EResult.k_EResultBanned => "your account isn't allowed to upload to the Workshop.",
        EResult.k_EResultNotLoggedOn => "you aren't signed in to Steam.",
        EResult.k_EResultTimeout => "Steam timed out. Try again later.",
        EResult.k_EResultServiceUnavailable or EResult.k_EResultServiceReadOnly or EResult.k_EResultBusy => "the Workshop is unavailable right now. Try again later.",
        EResult.k_EResultLimitExceeded => "a Steam size limit was exceeded. The preview image may be too large.",
        EResult.k_EResultInvalidParam => "Steam didn't accept one of the item's properties.",
        _ => result.ToString().Replace("k_EResult", "")
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        SteamAPI.Shutdown();
        Interlocked.Exchange(ref _openClients, 0);
    }
}
