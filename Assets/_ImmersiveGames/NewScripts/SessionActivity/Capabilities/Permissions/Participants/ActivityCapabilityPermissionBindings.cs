using System;
using UnityEngine;

/// <summary>
/// Componente agregador Unity-facing para declarar múltiplos bindings de permission por Actor/Object.
/// </summary>
[DisallowMultipleComponent]
public sealed class ActivityCapabilityPermissionBindings : MonoBehaviour
{
    [SerializeField]
    private ActivityCapabilityPermissionBindingEntry[] bindings = Array.Empty<ActivityCapabilityPermissionBindingEntry>();

    public ActivityCapabilityPermissionBindingEntry[] Bindings => bindings ?? Array.Empty<ActivityCapabilityPermissionBindingEntry>();
}
