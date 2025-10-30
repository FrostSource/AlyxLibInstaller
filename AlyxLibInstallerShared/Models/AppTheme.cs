using System.Text.Json.Serialization;

namespace AlyxLibInstallerShared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppTheme
{
    //None, // Turns off Fluent theming in WPF
    System,
    Light,
    Dark
}
