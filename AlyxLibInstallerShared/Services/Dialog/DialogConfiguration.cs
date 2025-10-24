using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Services.Dialog;
public record DialogConfiguration
{
    public string? Title { get; init; }
    public string? Message { get; init; }
    public DialogContext Context { get; init; } = DialogContext.Generic;
    public string? PrimaryButtonText { get; init; }
    public string? SecondaryButtonText { get; init; }
    public string? CancelButtonText { get; init; } = "OK";
    
    public bool HasCheckBox { get; init; } = false;
    public string? CheckBoxText { get; init; } = "Dont show this again";
    public bool CheckBoxDefaultChecked { get; init; } = false;

    public bool HasTextBox { get; init; } = false;
    public string? TextBoxPlaceholderText { get; init; }
    public string? TextBoxDefaultText { get; init; }
    public Func<string?, bool>? TextBoxValidator { get; init; } = null;
    public string TextBoxInvalidMessage { get; init; } = "Invalid input";

    public int? Width { get; init; }

    public DialogIconType IconType { get; init; } = DialogIconType.None;
    public string IconGlyph => IconType switch
    {
        DialogIconType.Information => "\uE946",
        DialogIconType.Warning => "\uE7BA",
        DialogIconType.Error => "\uEA39",
        _ => ""
    };
}
