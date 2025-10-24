using Microsoft.UI.Xaml.Controls;

namespace AlyxLibInstaller;
public class DialogResultWithData<T>
{
    public ContentDialogResult Result { get; set; }
    public T? Data { get; set; }
}
