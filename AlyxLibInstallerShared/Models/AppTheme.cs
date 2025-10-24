using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppTheme
{
    //None, // Turns off Fluent theming in WPF
    System,
    Light,
    Dark
}
