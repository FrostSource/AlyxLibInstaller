namespace AlyxLibInstallerWPF.Dialogs;

public enum ContentDialogResult
{
    None,
    Primary,
    Secondary
}

public class DialogResultWithData<T>
{
    public ContentDialogResult Result { get; set; }
    public T? Data { get; set; }
}
