using AlyxLibInstallerShared.Messaging.Payloads;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AlyxLibInstallerShared.Messaging.Messages;
public class AddonChangedMessage(AddonChangedPayload value)
    : ValueChangedMessage<AddonChangedPayload>(value);