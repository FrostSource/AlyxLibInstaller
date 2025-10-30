namespace AlyxLibInstallerShared.Services.Dialog;
public record DialogResponse(DialogResult Result, bool? CheckboxChecked = null, string? InputText = null);
