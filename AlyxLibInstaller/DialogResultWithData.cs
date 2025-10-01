using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstaller;
public class DialogResultWithData<T>
{
    public ContentDialogResult Result { get; set; }
    public T? Data { get; set; }
}
