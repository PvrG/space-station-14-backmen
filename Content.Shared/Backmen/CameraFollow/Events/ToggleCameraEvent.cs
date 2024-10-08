﻿using System.Numerics;
using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Backmen.CameraFollow.Events;

/// <summary>
/// Turns on/off the camera following the player's mouse position.
/// </summary>
public sealed partial class ToggleCameraEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ChangeCamOffsetEvent : EntityEventArgs
{
    public Vector2 Offset = Vector2.Zero;
}
