using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services;
using AlyxLibInstallerShared.Services.Dialog;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileDeployment;
using System.Collections.ObjectModel;

namespace AlyxLibInstallerShared.ViewModels;

public partial class WorkshopTagOption(string name, bool isSelected) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = isSelected;
}

public partial class WorkshopTagGroupOption : ObservableObject
{
    public const string NoChoice = "None";

    public string Name { get; }
    public IReadOnlyList<WorkshopTagOption> Tags { get; }

    /// <summary>
    /// Only one tag is chosen, from <see cref="Choices"/>.
    /// </summary>
    public bool SingleChoice { get; }

    public IReadOnlyList<string> Choices { get; }

    /// <summary>
    /// The chosen tag of a single choice group, or <see cref="NoChoice"/>. Choosing one clears the group's others.
    /// </summary>
    public string Choice
    {
        get => Tags.FirstOrDefault(tag => tag.IsSelected)?.Name ?? NoChoice;
        set
        {
            foreach (var tag in Tags)
                tag.IsSelected = tag.Name == value;
        }
    }

    public WorkshopTagGroupOption(string name, IReadOnlyList<WorkshopTagOption> tags, bool singleChoice = false)
    {
        Name = name;
        Tags = tags;
        SingleChoice = singleChoice;
        Choices = [NoChoice, .. tags.Select(tag => tag.Name)];
        foreach (var tag in tags)
            tag.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Choice));
    }
}

public record RequiredItemOption(ulong ItemId)
{
    public string Name => ItemId == WorkshopProperties.AlyxLibItemId ? $"AlyxLib ({ItemId})" : ItemId.ToString();
    public string Url => WorkshopLinks.GetItemUrl(ItemId);
}

/// <summary>
/// A property that's different on the Workshop than in the addon.
/// </summary>
public partial class WorkshopPropertyMismatch(WorkshopFields field, string name) : ObservableObject
{
    public WorkshopFields Field { get; } = field;
    public string Name { get; } = name;

    [ObservableProperty]
    public partial string WorkshopValue { get; set; } = "";

    [ObservableProperty]
    public partial bool CanUseWorkshopVersion { get; set; } = true;

    /// <summary>
    /// Replace the Workshop's value with the addon's when saving.
    /// </summary>
    [ObservableProperty]
    public partial bool UpdateWorkshop { get; set; }
}

/// <summary>
/// Edits an addon's Workshop item properties, showing where they differ from the Workshop.
/// </summary>
public partial class WorkshopPropertiesEditorViewModel : ObservableObject
{
    public const int MaxTitleLength = 128;
    public const int MaxDescriptionLength = 8000;

    private const string TextFileFilter = "Text files (*.txt;*.md)|*.txt;*.md|All files (*.*)|*.*";
    private const string ImageFileFilter = "Images (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp";

    private readonly string _contentPath;
    private readonly IFolderPickerService _picker;
    private readonly WorkshopItemDetails? _remote;
    private readonly bool _keepAlyxAddonTag;
    private string? _descriptionFileText;

    public ulong ItemId { get; }
    public string ItemUrl => WorkshopLinks.GetItemUrl(ItemId);

    /// <summary>
    /// The item hasn't been created on the Workshop yet.
    /// </summary>
    public bool IsNewItem => ItemId == 0;

    /// <summary>
    /// Why the Workshop's properties couldn't be loaded, if they couldn't.
    /// </summary>
    public string? RemoteError { get; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDescriptionFile))]
    public partial string? DescriptionFile { get; set; }

    public bool HasDescriptionFile => !string.IsNullOrWhiteSpace(DescriptionFile);

    [ObservableProperty]
    public partial string? DescriptionFileError { get; set; }

    [ObservableProperty]
    public partial WorkshopVisibility Visibility { get; set; }

    public IReadOnlyList<KeyValuePair<string, WorkshopVisibility>> VisibilityOptions { get; } =
    [
        new("Public", WorkshopVisibility.Public),
        new("Friends only", WorkshopVisibility.FriendsOnly),
        new("Unlisted", WorkshopVisibility.Unlisted),
        new("Private", WorkshopVisibility.Private),
    ];

    public IReadOnlyList<WorkshopTagGroupOption> TagGroups { get; }

    /// <summary>
    /// Groups shown as dropdowns, like Addon Type.
    /// </summary>
    public IEnumerable<WorkshopTagGroupOption> ChoiceTagGroups => TagGroups.Where(group => group.SingleChoice);

    /// <summary>
    /// Groups shown as checkboxes.
    /// </summary>
    public IEnumerable<WorkshopTagGroupOption> CheckboxTagGroups => TagGroups.Where(group => !group.SingleChoice);

    /// <summary>
    /// The checkbox tags chosen, since the dropdowns show their own.
    /// </summary>
    public string SelectedTagsSummary
    {
        get
        {
            var selected = CheckboxTagGroups.SelectMany(group => group.Tags).Where(tag => tag.IsSelected).Select(tag => tag.Name).ToList();
            return selected.Count == 0 ? "None" : string.Join(", ", selected);
        }
    }

    private IEnumerable<string> SelectedTags => TagGroups.SelectMany(group => group.Tags).Where(tag => tag.IsSelected).Select(tag => tag.Name);

    public ObservableCollection<RequiredItemOption> RequiredItems { get; }

    public bool IsAlyxLibRequired => RequiredItems.Any(item => item.ItemId == WorkshopProperties.AlyxLibItemId);

    [ObservableProperty]
    public partial string NewRequiredItemText { get; set; } = "";

    [ObservableProperty]
    public partial string? RequiredItemError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewSource))]
    [NotifyPropertyChangedFor(nameof(PreviewInfo))]
    public partial string? PreviewFile { get; set; }

    /// <summary>
    /// The chosen preview's name and size, or null if there isn't one.
    /// </summary>
    public string? PreviewInfo
    {
        get
        {
            if (PreviewFile == null)
                return null;
            var preview = new FileInfo(WorkshopProperties.ResolvePath(_contentPath, PreviewFile));
            if (!preview.Exists)
                return $"{preview.Name} wasn't found";
            var info = $"{preview.Name}, {Utils.FormatFileSize(preview.Length)}";
            return PreviewImages.NeedsConverting(preview.FullName) ? $"{info}. Shrunk to fit 1 MB when uploaded." : info;
        }
    }

    /// <summary>
    /// What will be uploaded, e.g. "120 files, 3.4 MB", or null when the editor isn't part of an upload.
    /// </summary>
    [ObservableProperty]
    public partial string? UploadSummary { get; set; }

    /// <summary>
    /// The preview was changed in this editor, so it's uploaded when saving.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsPreviewSaveNote))]
    public partial bool PreviewChanged { get; private set; }

    /// <summary>
    /// A new preview for an existing item is uploaded as soon as the properties are saved.
    /// </summary>
    public bool ShowsPreviewSaveNote => PreviewChanged && !IsNewItem;

    /// <summary>
    /// The preview to show: the chosen file, or the Workshop's current image.
    /// </summary>
    public string? PreviewSource => PreviewFile != null ? WorkshopProperties.ResolvePath(_contentPath, PreviewFile) : _remote?.PreviewUrl;

    public ObservableCollection<WorkshopPropertyMismatch> Mismatches { get; } = [];

    public bool HasMismatches => Mismatches.Count > 0;

    /// <summary>
    /// The properties marked to replace the Workshop's values when saving.
    /// </summary>
    public WorkshopFields FieldsToUpdate => Mismatches
        .Where(mismatch => mismatch.UpdateWorkshop)
        .Aggregate(WorkshopFields.None, (fields, mismatch) => fields | mismatch.Field);

    /// <summary>
    /// The description to compare and upload, read from the linked file if there is one.
    /// </summary>
    public string? CurrentDescription => HasDescriptionFile ? _descriptionFileText : Description;

    /// <param name="properties">The saved properties. Untracked ones start with the Workshop's values.</param>
    /// <param name="remote">The item's current Workshop properties, or null if they couldn't be loaded.</param>
    public WorkshopPropertiesEditorViewModel(WorkshopProperties properties, string contentPath, WorkshopItemDetails? remote, string? remoteError, IFolderPickerService picker)
    {
        _contentPath = contentPath;
        _picker = picker;
        _remote = remote;
        ItemId = properties.ItemId;
        RemoteError = remote == null ? remoteError : null;

        Title = properties.Title ?? remote?.Title ?? "";
        Description = properties.Description ?? remote?.Description ?? "";
        DescriptionFile = properties.DescriptionFile;
        ReadDescriptionFile();
        Visibility = properties.Visibility ?? remote?.Visibility ?? WorkshopVisibility.Private;
        PreviewFile = properties.PreviewFile;

        var tags = properties.Tags ?? remote?.Tags ?? [];
        _keepAlyxAddonTag = tags.Count == 0 || tags.Contains(WorkshopTags.AlyxAddon, StringComparer.OrdinalIgnoreCase);
        TagGroups = CreateTagGroups(tags, remote?.Tags ?? []);
        foreach (var tag in TagGroups.SelectMany(group => group.Tags))
        {
            tag.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(SelectedTagsSummary));
                RefreshMismatches();
            };
        }

        // A new item requires AlyxLib by default
        RequiredItems = [.. (properties.RequiredItems ?? remote?.RequiredItems ?? [WorkshopProperties.AlyxLibItemId]).Select(id => new RequiredItemOption(id))];
        RequiredItems.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsAlyxLibRequired));
            RefreshMismatches();
        };

        Mismatches.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMismatches));
        RefreshMismatches();
    }

    private static List<WorkshopTagGroupOption> CreateTagGroups(IReadOnlyList<string> selected, IReadOnlyList<string> remoteTags)
    {
        bool IsSelected(string tag) => selected.Contains(tag, StringComparer.OrdinalIgnoreCase);

        var groups = WorkshopTags.Groups
            .Select(group => new WorkshopTagGroupOption(group.Name, [.. group.Tags.Select(tag => new WorkshopTagOption(tag, IsSelected(tag)))], group.SingleChoice))
            .ToList();

        // Tags added another way, e.g. on the Workshop page, are kept so they can be chosen again
        var known = WorkshopTags.Groups.SelectMany(group => group.Tags).Append(WorkshopTags.AlyxAddon).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var other = selected.Concat(remoteTags).Where(tag => !known.Contains(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (other.Count > 0)
            groups.Add(new WorkshopTagGroupOption("Other", [.. other.Select(tag => new WorkshopTagOption(tag, IsSelected(tag)))]));

        return groups;
    }

    partial void OnTitleChanged(string value) => RefreshMismatches();
    partial void OnDescriptionChanged(string value) => RefreshMismatches();
    partial void OnVisibilityChanged(WorkshopVisibility value) => RefreshMismatches();

    partial void OnDescriptionFileChanged(string? value)
    {
        ReadDescriptionFile();
        RefreshMismatches();
    }

    private void ReadDescriptionFile()
    {
        _descriptionFileText = null;
        DescriptionFileError = null;
        if (!HasDescriptionFile)
            return;

        var path = WorkshopProperties.ResolvePath(_contentPath, DescriptionFile!);
        if (File.Exists(path))
            _descriptionFileText = File.ReadAllText(path);
        else
            DescriptionFileError = $"File not found: {path}";
    }

    /// <summary>
    /// Creates the properties to save, with every property tracked.
    /// </summary>
    public WorkshopProperties Build() => new()
    {
        ItemId = ItemId,
        Title = Title.Trim(),
        Description = Description,
        DescriptionFile = HasDescriptionFile ? DescriptionFile : null,
        Visibility = Visibility,
        Tags = [.. (_keepAlyxAddonTag ? [WorkshopTags.AlyxAddon] : Array.Empty<string>()).Concat(SelectedTags)],
        RequiredItems = [.. RequiredItems.Select(item => item.ItemId)],
        PreviewFile = PreviewFile
    };

    /// <summary>
    /// Checks the properties can be saved and uploaded.
    /// </summary>
    /// <returns>What needs fixing, or null if nothing does.</returns>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return "Enter a title.";
        if (Title.Trim().Length > MaxTitleLength)
            return $"The title is too long. It can be at most {MaxTitleLength} characters.";
        if (DescriptionFileError != null)
            return $"The linked description file can't be read. {DescriptionFileError}";
        if ((CurrentDescription?.Length ?? 0) > MaxDescriptionLength)
            return $"The description is too long. It can be at most {MaxDescriptionLength} characters.";

        // Steam doesn't show items without a preview properly
        if (IsNewItem && PreviewFile == null)
            return "Choose a preview image. New Workshop items need one.";

        if ((PreviewChanged || IsNewItem) && PreviewFile != null)
        {
            var preview = new FileInfo(WorkshopProperties.ResolvePath(_contentPath, PreviewFile));
            if (!preview.Exists)
                return $"The preview image can't be found: {preview.FullName}";
        }

        return null;
    }

    /// <summary>
    /// Updates the list of properties that differ from the Workshop, keeping each one's choice.
    /// </summary>
    private void RefreshMismatches()
    {
        // Runs from property setters during construction, before everything is set
        if (_remote == null || TagGroups == null || RequiredItems == null)
            return;

        var differences = WorkshopPropertyComparer.GetDifferences(Build(), CurrentDescription, _remote);

        foreach (var stale in Mismatches.Where(mismatch => !differences.HasFlag(mismatch.Field)).ToList())
            Mismatches.Remove(stale);

        foreach (var field in Enum.GetValues<WorkshopFields>().Where(field => field != WorkshopFields.None && differences.HasFlag(field)))
        {
            var mismatch = Mismatches.FirstOrDefault(existing => existing.Field == field);
            if (mismatch == null)
            {
                mismatch = new WorkshopPropertyMismatch(field, GetName(field));
                mismatch.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(WorkshopPropertyMismatch.UpdateWorkshop))
                        OnPropertyChanged(nameof(FieldsToUpdate));
                };
                Mismatches.Add(mismatch);
            }

            mismatch.WorkshopValue = DescribeWorkshopValue(field);
            // Copying into the user's own description file would overwrite it
            mismatch.CanUseWorkshopVersion = !(field == WorkshopFields.Description && HasDescriptionFile);
        }

        OnPropertyChanged(nameof(FieldsToUpdate));
    }

    private static string GetName(WorkshopFields field) => field switch
    {
        WorkshopFields.RequiredItems => "Required items",
        _ => field.ToString()
    };

    private string DescribeWorkshopValue(WorkshopFields field)
    {
        string List(IEnumerable<string> values)
        {
            var list = values.ToList();
            return list.Count == 0 ? "None" : string.Join(", ", list);
        }

        return field switch
        {
            WorkshopFields.Title => _remote!.Title,
            WorkshopFields.Description => Shorten(_remote!.Description),
            WorkshopFields.Visibility => VisibilityOptions.First(option => option.Value == _remote!.Visibility).Key,
            WorkshopFields.Tags => List(_remote!.Tags.Where(tag => !tag.Equals(WorkshopTags.AlyxAddon, StringComparison.OrdinalIgnoreCase))),
            WorkshopFields.RequiredItems => List(_remote!.RequiredItems.Select(id => new RequiredItemOption(id).Name)),
            _ => ""
        };
    }

    private static string Shorten(string text)
    {
        var firstLine = text.Trim().Split('\n')[0].Trim();
        if (firstLine.Length == 0)
            return "Empty";
        return firstLine.Length > 80 ? firstLine[..80] + "…" : firstLine;
    }

    [RelayCommand]
    private void UseWorkshopVersion(WorkshopPropertyMismatch mismatch)
    {
        if (_remote == null || !mismatch.CanUseWorkshopVersion)
            return;

        switch (mismatch.Field)
        {
            case WorkshopFields.Title:
                Title = _remote.Title;
                break;
            case WorkshopFields.Description:
                Description = _remote.Description;
                break;
            case WorkshopFields.Visibility:
                Visibility = _remote.Visibility;
                break;
            case WorkshopFields.Tags:
                foreach (var tag in TagGroups.SelectMany(group => group.Tags))
                    tag.IsSelected = _remote.Tags.Contains(tag.Name, StringComparer.OrdinalIgnoreCase);
                break;
            case WorkshopFields.RequiredItems:
                RequiredItems.Clear();
                foreach (var id in _remote.RequiredItems)
                    RequiredItems.Add(new RequiredItemOption(id));
                break;
        }
    }

    [RelayCommand]
    private void LinkDescriptionFile()
    {
        var path = _picker.PickFile("Choose a description file", TextFileFilter, _contentPath);
        if (path != null)
            DescriptionFile = ToStoredPath(path);
    }

    [RelayCommand]
    private void UnlinkDescriptionFile()
    {
        // The text box starts from the file's text, so nothing is lost
        if (_descriptionFileText != null)
            Description = _descriptionFileText;
        DescriptionFile = null;
    }

    [RelayCommand]
    private void AddAlyxLib()
    {
        if (!IsAlyxLibRequired)
            RequiredItems.Insert(0, new RequiredItemOption(WorkshopProperties.AlyxLibItemId));
    }

    [RelayCommand]
    private void AddRequiredItem()
    {
        if (!WorkshopLinks.TryParseItemId(NewRequiredItemText, out var itemId))
        {
            RequiredItemError = "Enter a Workshop item ID or link.";
            return;
        }
        if (itemId == ItemId)
        {
            RequiredItemError = "An item can't require itself.";
            return;
        }

        RequiredItemError = null;
        NewRequiredItemText = "";
        if (!RequiredItems.Any(item => item.ItemId == itemId))
            RequiredItems.Add(new RequiredItemOption(itemId));
    }

    [RelayCommand]
    private void RemoveRequiredItem(RequiredItemOption item) => RequiredItems.Remove(item);

    [RelayCommand]
    private void ChoosePreview()
    {
        var path = _picker.PickFile("Choose a preview image", ImageFileFilter, _contentPath);
        if (path == null)
            return;

        PreviewFile = ToStoredPath(path);
        PreviewChanged = true;
    }

    /// <summary>
    /// Stores files inside the addon relative to it, so the link survives the addon being moved.
    /// </summary>
    private string ToStoredPath(string path) =>
        FileUtils.IsInsideFolder(path, _contentPath) ? Path.GetRelativePath(_contentPath, path) : path;
}
