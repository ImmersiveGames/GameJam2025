using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityPermissionScanner : IActivityCapabilityScanner
    {
        private const string ModuleId = "SessionActivity.PermissionMovement";

        public string ScannerId => "activity_capability_permission_scanner.v1";
        public int Order => 300;

        public ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability scan context is invalid.");
            }

            ActivityCapabilityInventoryId inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActivityCapabilityDescriptor> capabilities = new();
            List<IActivityCapabilityRuntimeReference> runtimeReferences = new();
            HashSet<string> ownerKeys = new(StringComparer.Ordinal);
            HashSet<string> capabilityKeys = new(StringComparer.Ordinal);

            for (int index = 0; index < context.PlayerActorTargets.Count; index++)
            {
                ActivityCapabilityPlayerActorScanTarget target = context.PlayerActorTargets[index];
                if (!target.IsValid)
                {
                    continue;
                }

                string ownerPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(target.ActorRoot.transform);
                string ownerId = ActivityCapabilityInventoryId.DeriveOwnerId(
                    inventoryId,
                    ActivityCapabilityOwnerKind.PlayerActor,
                    ownerPath,
                    target.PlayerActorId);

                if (ownerKeys.Add(ownerId))
                {
                    owners.Add(new ActivityCapabilityOwnerDescriptor(
                        ActivityCapabilityOwnerKind.PlayerActor,
                        ownerId,
                        ownerPath,
                        target.SourceScene,
                        target.SourceContent,
                        context.Source));
                }

                PlayerMovementController[] controllers = target.ActorRoot.GetComponentsInChildren<PlayerMovementController>(true);
                for (int controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
                {
                    PlayerMovementController controller = controllers[controllerIndex];
                    if (controller == null)
                    {
                        continue;
                    }

                    string componentPath = ActivityCapabilityTransformPathUtility.BuildTransformPath(controller.transform);
                    string componentType = controller.GetType().FullName ?? controller.GetType().Name;
                    string capabilityId = ActivityCapabilityInventoryId.DeriveCapabilityId(
                        inventoryId,
                        ownerId,
                        ActivityCapabilityKind.PermissionTarget,
                        ModuleId,
                        componentPath);

                    if (!capabilityKeys.Add(capabilityId))
                    {
                        continue;
                    }

                    string permissionToken = ActivityCapabilityPermissionIds.ActivityGameplayControl;
                    ActivityCapabilityPermissionReceiverIdentity receiverIdentity = new(
                        context.Identity.PipelineId,
                        context.Identity.SessionId,
                        context.Identity.ActivityId,
                        context.Identity.EntrySequence,
                        target.PlayerActorId,
                        target.PlayerSlotId);
                    string receiverId = PlayerMovementPermissionReceiver.CreateReceiverId(
                        receiverIdentity);

                    PlayerMovementPermissionReceiver receiver = new(
                        controller,
                        receiverId,
                        target.PlayerActorId,
                        target.PlayerSlotId);

                    capabilities.Add(new ActivityCapabilityDescriptor(
                        capabilityId,
                        ActivityCapabilityKind.PermissionTarget,
                        ModuleId,
                        ownerId,
                        componentPath,
                        componentType,
                        required: true,
                        priority: 100,
                        policyMetadata: new[]
                        {
                            new ActivityCapabilityPolicyEntry("permissionId", permissionToken),
                            new ActivityCapabilityPolicyEntry("playerActorId", target.PlayerActorId),
                            new ActivityCapabilityPolicyEntry("playerSlotId", target.PlayerSlotId),
                        },
                        source: context.Source));

                    runtimeReferences.Add(new ActivityCapabilityPermissionReceiverReference(
                        capabilityId,
                        ownerId,
                        target.PlayerActorId,
                        componentPath,
                        permissionToken,
                        receiverIdentity,
                        receiver));
                }
            }

            return new ActivityCapabilityScanResult(ScannerId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }
    }

}
