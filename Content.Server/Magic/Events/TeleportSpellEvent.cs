﻿using Content.Shared.Actions;
using Content.Shared.Sound;

namespace Content.Server.Magic.Events;

public sealed class TeleportSpellEvent : WorldTargetActionEvent
{
    [DataField("blinkSound")]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");
}
