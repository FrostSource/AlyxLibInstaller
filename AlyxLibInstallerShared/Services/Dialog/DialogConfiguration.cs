using static System.Net.Mime.MediaTypeNames;

namespace AlyxLibInstallerShared.Services.Dialog;
public record DialogConfiguration
{
    public string? Title { get; init; }
    public string? Message { get; init; }
    public DialogContext? Context { get; init; }
    public string? PrimaryButtonText { get; init; }
    public string? SecondaryButtonText { get; init; }
    public string? CancelButtonText { get; init; }

    public bool? HasCheckBox { get; init; }
    public string? CheckBoxText { get; init; }
    public bool? CheckBoxDefaultChecked { get; init; }

    public bool? HasTextBox { get; init; }
    public string? TextBoxPlaceholderText { get; init; }
    public string? TextBoxDefaultText { get; init; }
    public Func<string?, bool>? TextBoxValidator { get; init; }
    public string? TextBoxInvalidMessage { get; init; }

    public int? Width { get; init; }

    public DialogIconType? IconType { get; init; }
    public string IconGlyph => IconType switch
    {
        DialogIconType.Information => "\uE946",
        DialogIconType.Warning => "\uE7BA",
        DialogIconType.Error => "\uEA39",
        _ => ""
    };

    public static DialogConfiguration Defaults => new()
    {
        Context = DialogContext.Generic,
        CancelButtonText = "OK",
        HasCheckBox = false,
        CheckBoxText = "Dont show this again",
        CheckBoxDefaultChecked = false,
        HasTextBox = false,
        TextBoxInvalidMessage = "Invalid input",
        IconType = DialogIconType.None
    };

    public DialogConfiguration With(DialogConfiguration other) => new()
    {
        Title = other.Title ?? Title,
        Message = other.Message ?? Message,
        Context = other.Context ?? Context,
        PrimaryButtonText = other.PrimaryButtonText ?? PrimaryButtonText,
        SecondaryButtonText = other.SecondaryButtonText ?? SecondaryButtonText,
        CancelButtonText = other.CancelButtonText ?? CancelButtonText,
        HasCheckBox = other.HasCheckBox ?? HasCheckBox,
        CheckBoxText = other.CheckBoxText ?? CheckBoxText,
        CheckBoxDefaultChecked = other.CheckBoxDefaultChecked,
        HasTextBox = other.HasTextBox ?? HasTextBox,
        TextBoxPlaceholderText = other.TextBoxPlaceholderText ?? TextBoxPlaceholderText,
        TextBoxDefaultText = other.TextBoxDefaultText ?? TextBoxDefaultText,
        TextBoxValidator = other.TextBoxValidator ?? TextBoxValidator,
        TextBoxInvalidMessage = other.TextBoxInvalidMessage ?? TextBoxInvalidMessage,
        Width = other.Width ?? Width,
        IconType = other.IconType ?? IconType
    };

}
