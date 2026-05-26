using UnityEngine;

/// <summary>
/// Registro deterministico de um binding-target descoberto em um Actor/Object.
/// O descriptor e produzido por discovery; ele nao publica permission e nao executa lifecycle macro.
/// </summary>
public readonly struct ActivityCapabilityPermissionParticipantDescriptor
{
    public ActivityCapabilityPermissionParticipantDescriptor(
        ActivityCapabilityPermissionBindings bindingsComponent,
        MonoBehaviour reactionTarget,
        string ownerId,
        string ownerPath,
        string participantId,
        string permissionId,
        bool required,
        int bindingIndex,
        int targetIndex,
        string orderKey)
    {
        BindingsComponent = bindingsComponent;
        ReactionTarget = reactionTarget;
        OwnerId = ownerId ?? string.Empty;
        OwnerPath = ownerPath ?? string.Empty;
        ParticipantId = participantId ?? string.Empty;
        PermissionId = permissionId ?? string.Empty;
        Required = required;
        BindingIndex = bindingIndex < 0 ? 0 : bindingIndex;
        TargetIndex = targetIndex < 0 ? 0 : targetIndex;
        OrderKey = orderKey ?? string.Empty;
    }

    public ActivityCapabilityPermissionBindings BindingsComponent { get; }
    public MonoBehaviour ReactionTarget { get; }
    public string OwnerId { get; }
    public string OwnerPath { get; }
    public string ParticipantId { get; }
    public string PermissionId { get; }
    public bool Required { get; }
    public int BindingIndex { get; }
    public int TargetIndex { get; }
    public string OrderKey { get; }

    public bool IsValid =>
        BindingsComponent != null &&
        ReactionTarget != null &&
        ReactionTarget is IActivityCapabilityPermissionReactionTarget &&
        !string.IsNullOrWhiteSpace(ParticipantId) &&
        !string.IsNullOrWhiteSpace(PermissionId);

    public override string ToString()
    {
        return $"ownerId='{OwnerId}', ownerPath='{OwnerPath}', participantId='{ParticipantId}', permissionId='{PermissionId}', required='{Required}', bindingIndex='{BindingIndex}', targetIndex='{TargetIndex}', orderKey='{OrderKey}'";
    }
}
