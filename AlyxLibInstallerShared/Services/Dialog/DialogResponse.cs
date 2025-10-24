using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Services.Dialog;
public record DialogResponse(DialogResult Result, bool? CheckboxChecked = null, string? InputText = null);
