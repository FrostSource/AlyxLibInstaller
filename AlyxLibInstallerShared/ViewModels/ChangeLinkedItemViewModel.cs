using AlyxLib.Workshop;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlyxLibInstallerShared.ViewModels;

public partial class LinkableWorkshopItem(PublishData data, bool isSelected) : ObservableObject
{
    public ulong ItemId { get; } = data.ItemId;
    public string Title { get; } = data.Title;
    public string Published { get; } = data.PublishTime == DateTimeOffset.MinValue ? "" : $"Last uploaded {data.PublishTime.ToLocalTime():g}";

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = isSelected;
}

/// <summary>
/// Picks the Workshop item an addon is linked to, from the items it was published as or by ID.
/// </summary>
public partial class ChangeLinkedItemViewModel : ObservableObject
{
    public IReadOnlyList<LinkableWorkshopItem> Items { get; }

    public bool HasItems => Items.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItemId))]
    public partial bool UseOtherItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItemId))]
    public partial string OtherItemText { get; set; } = "";

    public ChangeLinkedItemViewModel(IEnumerable<PublishData> items, ulong? linkedItemId)
    {
        Items = [.. items.Select(item => new LinkableWorkshopItem(item, item.ItemId == linkedItemId))];

        // An item linked by ID that wasn't published from this addon
        if (linkedItemId is ulong id && !Items.Any(item => item.IsSelected))
        {
            UseOtherItem = true;
            OtherItemText = id.ToString();
        }

        foreach (var item in Items)
        {
            item.PropertyChanged += (_, _) =>
            {
                if (item.IsSelected)
                {
                    UseOtherItem = false;
                    foreach (var other in Items.Where(other => other != item))
                        other.IsSelected = false;
                }
                OnPropertyChanged(nameof(SelectedItemId));
            };
        }
    }

    partial void OnOtherItemTextChanged(string value)
    {
        // Typing an ID chooses it
        if (!string.IsNullOrWhiteSpace(value))
            UseOtherItem = true;
    }

    partial void OnUseOtherItemChanged(bool value)
    {
        if (value)
        {
            foreach (var item in Items)
                item.IsSelected = false;
        }
    }

    public ulong? SelectedItemId => UseOtherItem
        ? WorkshopLinks.TryParseItemId(OtherItemText, out var id) ? id : null
        : Items.FirstOrDefault(item => item.IsSelected)?.ItemId;
}
