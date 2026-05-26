using System;
using UnityEngine;

/// <summary>
/// Entrada autoral de binding local entre uma permission de Activity e targets reativos.
/// </summary>
[Serializable]
public sealed class ActivityCapabilityPermissionBindingEntry
{
    [SerializeField]
    private string participantIdSeed;

    [SerializeField]
    private string permissionId = "activity.gameplay.control";

    [SerializeField]
    private bool required = true;

    [SerializeField]
    private MonoBehaviour[] reactionTargets = Array.Empty<MonoBehaviour>();

    public string ParticipantIdSeed => string.IsNullOrWhiteSpace(participantIdSeed) ? string.Empty : participantIdSeed.Trim();
    public string PermissionId => string.IsNullOrWhiteSpace(permissionId) ? string.Empty : permissionId.Trim();
    public bool Required => required;
    public MonoBehaviour[] ReactionTargets => reactionTargets ?? Array.Empty<MonoBehaviour>();
}
