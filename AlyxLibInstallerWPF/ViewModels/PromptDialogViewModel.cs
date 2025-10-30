using AlyxLibInstallerShared.Services.Dialog;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlyxLibInstallerWPF.ViewModels;
public partial class PromptDialogViewModel : ObservableObject
{
    // Config

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial bool HasCheckBox { get; set; } = false;
    [ObservableProperty]
    public partial string? CheckBoxText { get; set; } = "Dont show again";

    [ObservableProperty]
    public partial bool HasTextBox { get; set; } = false;
    [ObservableProperty]
    public partial string? TextBoxPlaceholderText { get; set; }
    [ObservableProperty]
    public partial string? TextBoxDefaultText { get; set; }
    [ObservableProperty]
    public partial Func<string?, bool>? TextBoxValidator { get; set; } = null;
    [ObservableProperty]
    public partial string TextBoxInvalidMessage { get; set; }
    [ObservableProperty]
    public partial double? DialogWidth { get; set; }

    // UI State

    [ObservableProperty]
    public partial bool IsCheckBoxChecked { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextBoxTextIsValid))]
    [NotifyPropertyChangedFor(nameof(FinalTextBoxText))]
    public partial string TextBoxText { get; set; }

    /// <summary>
    /// Returns the text in the text box, or the placeholder text if the text box is empty
    /// </summary>
    public string FinalTextBoxText => string.IsNullOrEmpty(TextBoxText) ? (TextBoxPlaceholderText ?? "") : TextBoxText;

    public bool TextBoxTextIsValid => TextBoxValidator == null || TextBoxValidator(FinalTextBoxText);

    public PromptDialogViewModel(DialogConfiguration config)
    {
        Message = config.Message;
        HasCheckBox = config.HasCheckBox;
        CheckBoxText = config.CheckBoxText;
        HasTextBox = config.HasTextBox;
        TextBoxPlaceholderText = config.TextBoxPlaceholderText;
        TextBoxDefaultText = config.TextBoxDefaultText;
        TextBoxValidator = config.TextBoxValidator;
        TextBoxInvalidMessage = config.TextBoxInvalidMessage;
        DialogWidth = config.Width;

        TextBoxText = TextBoxDefaultText ?? "";
        IsCheckBoxChecked = config.CheckBoxDefaultChecked;
    }
}
