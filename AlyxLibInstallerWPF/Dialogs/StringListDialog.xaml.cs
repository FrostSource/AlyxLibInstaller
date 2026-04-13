using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace AlyxLibInstallerWPF.Dialogs;
/// <summary>
/// Interaction logic for ListView.xaml
/// </summary>
public partial class StringListView : UserControl, INotifyPropertyChanged
{
    private IEnumerable<string> _stringList = [];
    private IReadOnlyList<StringListItem> _displayItems = [];
    private string? _message = "";
    private Visibility _actionLegendVisibility = Visibility.Collapsed;

    public IEnumerable<string> StringList
    {
        get => _stringList;
        set
        {
            _stringList = value ?? [];
            DisplayItems = _stringList.Select(CreateItem).ToList();
            ActionLegendVisibility = DisplayItems.Any(item => item.Action != StringListItemAction.None)
                ? Visibility.Visible
                : Visibility.Collapsed;
            OnPropertyChanged(nameof(StringList));
        }
    }

    public IReadOnlyList<StringListItem> DisplayItems
    {
        get => _displayItems;
        private set
        {
            _displayItems = value;
            OnPropertyChanged(nameof(DisplayItems));
        }
    }

    public string? Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged(nameof(Message));
        }
    }

    public Visibility ActionLegendVisibility
    {
        get => _actionLegendVisibility;
        private set
        {
            _actionLegendVisibility = value;
            OnPropertyChanged(nameof(ActionLegendVisibility));
        }
    }

    public StringListView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public StringListView(IList<string> stringList) : this()
    {
        StringList = stringList;
    }

    public StringListView(IList<string> stringList, string message) : this(stringList)
    {
        Message = message;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static StringListItem CreateItem(string rawValue)
    {
        const string deletePrefix = "del|";
        const string temporaryPrefix = "tmp|";

        if (rawValue.StartsWith(deletePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new StringListItem(
                rawValue[deletePrefix.Length..],
                StringListItemAction.Delete,
                "Delete: this file will be removed permanently during Remove For Upload.");
        }

        if (rawValue.StartsWith(temporaryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new StringListItem(
                rawValue[temporaryPrefix.Length..],
                StringListItemAction.TemporaryMove,
                "Temp Move: this file will be restored the next time you click Install.");
        }

        return new StringListItem(rawValue, StringListItemAction.None, string.Empty);
    }
}

public sealed class StringListItem
{
    public StringListItem(string text, StringListItemAction action, string actionToolTip)
    {
        Text = text;
        Action = action;
        ActionToolTip = actionToolTip;
    }

    public string Text { get; }
    public StringListItemAction Action { get; }
    public string ActionToolTip { get; }
}

public enum StringListItemAction
{
    None,
    Delete,
    TemporaryMove
}
