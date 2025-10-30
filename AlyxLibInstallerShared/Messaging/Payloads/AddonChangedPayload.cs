using AlyxLib;
using Source2HelperLibrary;

namespace AlyxLibInstallerShared.Messaging.Payloads;
public record AddonChangedPayload(
    LocalAddon NewAddon,
    AddonConfig? Config,
    bool IsDefaultConfig,
    bool AddonHasAlyxLib
);
