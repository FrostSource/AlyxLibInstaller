using AlyxLib;
using Source2HelperLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Messaging.Payloads;
public record AddonChangedPayload(
    LocalAddon NewAddon,
    AddonConfig? Config,
    bool IsDefaultConfig,
    bool AddonHasAlyxLib
);
