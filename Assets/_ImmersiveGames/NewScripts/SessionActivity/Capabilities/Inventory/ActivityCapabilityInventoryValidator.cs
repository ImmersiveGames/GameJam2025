using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityInventoryValidator
    {
        public ActivityCapabilityInventoryValidationResult Validate(ActivityCapabilityInventory inventory, string source, string reason)
        {
            List<ActivityCapabilityInventoryValidationIssue> issues = new();
            HashSet<string> owners = new(StringComparer.Ordinal);
            HashSet<string> semanticKeys = new(StringComparer.Ordinal);

            if (!inventory.Id.IsValid)
            {
                issues.Add(Issue("inventory_id_invalid", error: true, detail: "InventoryId is invalid."));
            }

            for (int ownerIndex = 0; ownerIndex < inventory.Owners.Count; ownerIndex++)
            {
                ActivityCapabilityOwnerDescriptor owner = inventory.Owners[ownerIndex];
                if (string.IsNullOrWhiteSpace(owner.OwnerId))
                {
                    issues.Add(Issue("owner_id_missing", error: true, detail: $"Owner at index '{ownerIndex}' has empty ownerId."));
                }
                else
                {
                    owners.Add(owner.OwnerId);
                }

                if (owner.OwnerKind == ActivityCapabilityOwnerKind.Unsupported)
                {
                    issues.Add(Issue("owner_kind_unsupported", error: false, ownerId: owner.OwnerId, detail: $"Owner '{owner.OwnerId}' has Unsupported ownerKind."));
                }
            }

            for (int capabilityIndex = 0; capabilityIndex < inventory.Capabilities.Count; capabilityIndex++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[capabilityIndex];

                if (string.IsNullOrWhiteSpace(capability.CapabilityId))
                {
                    issues.Add(Issue("capability_id_missing", error: true, ownerId: capability.OwnerId, detail: $"Capability at index '{capabilityIndex}' has empty capabilityId."));
                }

                if (string.IsNullOrWhiteSpace(capability.OwnerId) || !owners.Contains(capability.OwnerId))
                {
                    issues.Add(Issue("capability_owner_missing", error: true, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: "Capability ownerId does not resolve to any owner descriptor."));
                }

                if (capability.CapabilityKind == ActivityCapabilityKind.Unknown)
                {
                    issues.Add(Issue("capability_kind_unsupported", error: false, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: $"Capability kind '{capability.CapabilityKind}' is unsupported for passive validation."));
                }

                if (string.IsNullOrWhiteSpace(capability.ModuleId))
                {
                    issues.Add(Issue("capability_module_missing", error: true, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: "Capability moduleId is empty."));
                }

                if (string.IsNullOrWhiteSpace(capability.ComponentPath))
                {
                    issues.Add(Issue("capability_component_path_missing", error: true, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: "Capability componentPath is empty."));
                }

                if (string.IsNullOrWhiteSpace(capability.ComponentType))
                {
                    issues.Add(Issue("capability_component_type_missing", error: true, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: "Capability componentType is empty."));
                }

                string semanticKey = $"{capability.OwnerId}|{capability.CapabilityKind}|{capability.ModuleId}|{capability.ComponentPath}";
                if (!semanticKeys.Add(semanticKey))
                {
                    issues.Add(Issue("capability_semantic_duplicate", error: false, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: $"Duplicate semantic capability key '{semanticKey}'."));
                }

                if (capability.Required && (string.IsNullOrWhiteSpace(capability.OwnerId) || !owners.Contains(capability.OwnerId)))
                {
                    issues.Add(Issue("required_capability_without_owner", error: true, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: "Required capability has no valid owner."));
                }

                if (RequiresRuntimeReference(capability.CapabilityKind))
                {
                    if (!inventory.RuntimeReferences.TryGetValue(capability.CapabilityId, out IActivityCapabilityRuntimeReference runtimeReference) ||
                        runtimeReference == null)
                    {
                        issues.Add(Issue("runtime_reference_missing", error: true, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: $"Capability kind '{capability.CapabilityKind}' requires runtime reference."));
                    }
                    else if (!IsExpectedRuntimeReferenceType(capability.CapabilityKind, runtimeReference))
                    {
                        issues.Add(Issue("runtime_reference_type_mismatch", error: true, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: $"Capability kind '{capability.CapabilityKind}' runtime reference type '{runtimeReference.GetType().FullName}' is invalid."));
                    }
                }

                for (int policyIndex = 0; policyIndex < capability.PolicyMetadata.Count; policyIndex++)
                {
                    if (string.IsNullOrWhiteSpace(capability.PolicyMetadata[policyIndex].Key))
                    {
                        issues.Add(Issue("policy_key_missing", error: false, ownerId: capability.OwnerId, capabilityId: capability.CapabilityId, detail: $"Policy metadata key empty at index '{policyIndex}'."));
                    }
                }
            }

            ActivityCapabilityInventoryValidationStatus status = ResolveStatus(issues);
            return new ActivityCapabilityInventoryValidationResult(inventory, status, issues, source, reason);
        }

        private static ActivityCapabilityInventoryValidationStatus ResolveStatus(IReadOnlyList<ActivityCapabilityInventoryValidationIssue> issues)
        {
            bool hasErrors = false;
            bool hasWarnings = false;
            for (int index = 0; index < issues.Count; index++)
            {
                if (issues[index].IsError)
                {
                    hasErrors = true;
                }
                else
                {
                    hasWarnings = true;
                }
            }

            if (hasErrors)
            {
                return ActivityCapabilityInventoryValidationStatus.FailedPassive;
            }

            return hasWarnings
                ? ActivityCapabilityInventoryValidationStatus.PassedWithWarnings
                : ActivityCapabilityInventoryValidationStatus.Passed;
        }

        private static ActivityCapabilityInventoryValidationIssue Issue(
            string code,
            bool error,
            string ownerId = "",
            string capabilityId = "",
            string detail = "")
        {
            return new ActivityCapabilityInventoryValidationIssue(code, error, ownerId, capabilityId, detail);
        }

        private static bool RequiresRuntimeReference(ActivityCapabilityKind capabilityKind)
        {
            return capabilityKind == ActivityCapabilityKind.ResetEndpoint ||
                   capabilityKind == ActivityCapabilityKind.SnapshotProvider ||
                   capabilityKind == ActivityCapabilityKind.SnapshotRestoreEndpoint ||
                   capabilityKind == ActivityCapabilityKind.ReleaseEndpoint ||
                   capabilityKind == ActivityCapabilityKind.PermissionTarget ||
                   capabilityKind == ActivityCapabilityKind.AttributeEndpoint ||
                   capabilityKind == ActivityCapabilityKind.ProjectileEmitter ||
                   capabilityKind == ActivityCapabilityKind.PresentationEndpoint ||
                   capabilityKind == ActivityCapabilityKind.CameraTarget;
        }

        private static bool IsExpectedRuntimeReferenceType(ActivityCapabilityKind capabilityKind, IActivityCapabilityRuntimeReference runtimeReference)
        {
            return capabilityKind switch
            {
                ActivityCapabilityKind.ResetEndpoint => runtimeReference is ActivityObjectResetEndpointReference resetReference && resetReference.Endpoint != null,
                ActivityCapabilityKind.SnapshotProvider => runtimeReference is ActivityObjectSnapshotProviderReference snapshotReference && snapshotReference.Provider != null,
                ActivityCapabilityKind.SnapshotRestoreEndpoint => runtimeReference is ActivityObjectSnapshotRestoreEndpointReference restoreReference && restoreReference.Endpoint != null,
                ActivityCapabilityKind.ReleaseEndpoint => runtimeReference is ActivityObjectReleaseEndpointReference releaseReference && releaseReference.Endpoint != null,
                ActivityCapabilityKind.PermissionTarget => runtimeReference is ActivityCapabilityPermissionReceiverReference permissionReference &&
                                                           permissionReference.Receiver != null &&
                                                           permissionReference.PermissionId != ActivityCapabilityPermissionId.Unknown,
                ActivityCapabilityKind.AttributeEndpoint => runtimeReference is ActorAttributeEndpointReference attributeReference &&
                                                            attributeReference.Endpoint != null &&
                                                            !string.IsNullOrWhiteSpace(attributeReference.ActorId),
                ActivityCapabilityKind.ProjectileEmitter => runtimeReference is ActorProjectileEmitterEndpointReference projectileReference &&
                                                             projectileReference.Endpoint != null &&
                                                             !string.IsNullOrWhiteSpace(projectileReference.ActorId),
                ActivityCapabilityKind.PresentationEndpoint => runtimeReference is ActorPresentationEndpointReference presentationReference &&
                                                               presentationReference.Endpoint != null &&
                                                               !string.IsNullOrWhiteSpace(presentationReference.ActorId),
                ActivityCapabilityKind.CameraTarget => runtimeReference is ActivityCameraTargetReference cameraReference &&
                                                      cameraReference.TrackingTarget != null,
                _ => true,
            };
        }
    }
}
