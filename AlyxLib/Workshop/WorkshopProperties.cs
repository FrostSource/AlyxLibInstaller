using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlyxLib.Workshop;

/// <summary>
/// The local copy of an addon's Workshop item properties, saved in the addon's .alyxlib folder.
/// A property left null isn't tracked locally and is never compared or sent to Steam.
/// </summary>
public class WorkshopProperties
{
    public const string FileName = "workshop.json";

    /// <summary>
    /// The AlyxLib Workshop item, required by default for addons using AlyxLib.
    /// </summary>
    public const ulong AlyxLibItemId = 3329679071;

    public ulong ItemId { get; set; }
    public string? Title { get; set; }

    /// <summary>
    /// The description text. Ignored when <see cref="DescriptionFile"/> is set.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// A text file to use as the description, relative to the addon content folder or absolute.
    /// </summary>
    public string? DescriptionFile { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<WorkshopVisibility>))]
    public WorkshopVisibility? Visibility { get; set; }

    public List<string>? Tags { get; set; }
    public List<ulong>? RequiredItems { get; set; }

    /// <summary>
    /// The preview image, relative to the addon content folder or absolute.
    /// </summary>
    public string? PreviewFile { get; set; }

    [JsonIgnore]
    public bool HasDescriptionFile => !string.IsNullOrWhiteSpace(DescriptionFile);

    /// <summary>
    /// False for a new item whose properties are set up but which hasn't been created on the Workshop yet.
    /// </summary>
    [JsonIgnore]
    public bool HasItem => ItemId != 0;

    public static string GetPath(string contentPath) => Path.Combine(contentPath, ".alyxlib", FileName);

    /// <summary>
    /// Resolves a path stored relative to the addon content folder.
    /// </summary>
    public static string ResolvePath(string contentPath, string path) => Path.GetFullPath(Path.Combine(contentPath, path));

    /// <summary>
    /// Gets the description, reading it from the linked file if there is one.
    /// </summary>
    /// <exception cref="FileNotFoundException">The linked description file doesn't exist.</exception>
    public string? ReadDescription(string contentPath)
    {
        if (!HasDescriptionFile)
            return Description;

        var path = ResolvePath(contentPath, DescriptionFile!);
        if (!File.Exists(path))
            throw new FileNotFoundException("Linked description file not found.", path);

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Loads the properties for an addon, or returns null if the addon isn't linked to an item.
    /// </summary>
    public static WorkshopProperties? Load(string contentPath)
    {
        var path = GetPath(contentPath);
        if (!File.Exists(path))
            return null;

        return JsonSerializer.Deserialize(File.ReadAllText(path), WorkshopPropertiesJsonContext.Default.WorkshopProperties);
    }

    public void Save(string contentPath)
    {
        var path = GetPath(contentPath);
        var folder = Path.GetDirectoryName(path)!;

        // Matches the config file, which keeps the folder hidden
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder).Attributes |= FileAttributes.Hidden;

        File.WriteAllText(path, JsonSerializer.Serialize(this, WorkshopPropertiesJsonContext.Default.WorkshopProperties));
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WorkshopProperties))]
public partial class WorkshopPropertiesJsonContext : JsonSerializerContext
{
}
