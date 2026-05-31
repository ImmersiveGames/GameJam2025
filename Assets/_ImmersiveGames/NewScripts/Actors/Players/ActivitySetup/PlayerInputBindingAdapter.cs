using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class PlayerInputBindingAdapter : IPlayerInputBindingAdapter
    {
        public IReadOnlyList<PlayerInputBindingRecord> Execute(
            PlayerInputBindingCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("PlayerInputBindingCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for player input binding.");
            }

            if (!IsSameActivityCycle(command.PipelineIdentity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_player_input_binding_command: command identity does not match active identity.");
            }

            if (registry == null)
            {
                throw new InvalidOperationException("PlayerInputBindingAdapter requires non-null registry.");
            }

            List<PlayerInputBindingRecord> records = new(command.Requirements.Count);
            for (int index = 0; index < command.Requirements.Count; index++)
            {
                PlayerInputBindingRequirement requirement = command.Requirements[index];
                if (!requirement.IsValid)
                {
                    throw new InvalidOperationException($"PlayerInputBindingRequirement at index '{index}' is invalid.");
                }

                if (!IsSameActivityCycle(requirement.Identity, activeIdentity))
                {
                    throw new InvalidOperationException("stale_or_foreign_player_input_binding_requirement: requirement identity does not match active identity.");
                }

                PlayerActorRuntimeHandle actorHandle = registry.ResolveActiveHandleOrFail(activeIdentity, requirement.ParticipantId);
                EnsureHandleMatchesRequirement(actorHandle, requirement);

                GameObject actorInstance = actorHandle.Instance;
                InputActionAsset canonicalActionsAsset = ResolveCanonicalActionsAssetOrFail();
                PlayerInputResolution resolution = ResolvePlayerInputFromActorOrFail(actorInstance, actorHandle, requirement, canonicalActionsAsset);
                PlayerInput resolvedInput = resolution.PlayerInput;
                PlayerActorInputBindingState bindingState = actorInstance.GetComponent<PlayerActorInputBindingState>();
                if (bindingState == null)
                {
                    bindingState = actorInstance.AddComponent<PlayerActorInputBindingState>();
                }

                bindingState.Bind(
                    activeIdentity,
                    actorHandle.PlayerSlotId,
                    actorHandle.PlayerActorId,
                    resolvedInput.GetInstanceID(),
                    resolvedInput.playerIndex,
                    resolvedInput.name);

                records.Add(new PlayerInputBindingRecord(requirement, actorHandle.ActorIdentity, bound: true, observedInputId: $"{resolvedInput.name}|index={resolvedInput.playerIndex}|instance={resolvedInput.GetInstanceID()}|actionsRebound={resolution.ActionsReboundToCanonical}"));
            }

            return records;
        }

        private static void EnsureHandleMatchesRequirement(PlayerActorRuntimeHandle actorHandle, PlayerInputBindingRequirement requirement)
        {
            if (!actorHandle.IsValid)
            {
                throw new InvalidOperationException($"stale_or_foreign_player_input_binding_requirement: invalid handle participantId='{requirement.ParticipantId}'.");
            }

            if (actorHandle.ParticipantId != requirement.ParticipantId ||
                actorHandle.PlayerSlotId != requirement.PlayerSlotId ||
                actorHandle.ActorDefinitionId != requirement.ActorDefinitionId ||
                actorHandle.ActorId != requirement.ActorId)
            {
                throw new InvalidOperationException(
                    $"stale_or_foreign_player_input_binding_requirement: handle mismatch participantId='{requirement.ParticipantId}' handleParticipantId='{actorHandle.ParticipantId}' handlePlayerActorId='{actorHandle.PlayerActorId}' handlePlayerSlotId='{actorHandle.PlayerSlotId}'.");
            }
        }

        private static InputActionAsset ResolveCanonicalActionsAssetOrFail()
        {
            if (!RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) || snapshot == null)
            {
                throw new InvalidOperationException("PlayerInput binding failed: RuntimeConfigRegistry snapshot ausente.");
            }

            InputActionAsset asset = snapshot.InputModesRuntime?.UiActionsAsset;
            if (asset == null)
            {
                throw new InvalidOperationException("PlayerInput binding failed: InputModesRuntime.UiActionsAsset canonico ausente.");
            }

            if (asset.FindActionMap(InputModesDefaults.PlayerActionMapName, throwIfNotFound: false) == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: InputModesRuntime.UiActionsAsset canonico sem ActionMap '{InputModesDefaults.PlayerActionMapName}'.");
            }

            return asset;
        }

        private static PlayerInputResolution ResolvePlayerInputFromActorOrFail(
            GameObject actorInstance,
            PlayerActorRuntimeHandle actorHandle,
            PlayerInputBindingRequirement requirement,
            InputActionAsset canonicalActionsAsset)
        {
            if (actorInstance == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: actor instance ausente para playerActorId='{actorHandle.PlayerActorId}'.");
            }

            PlayerInput[] inputs = actorInstance.GetComponentsInChildren<PlayerInput>(includeInactive: true);
            if (inputs == null || inputs.Length == 0)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: playerActorId='{actorHandle.PlayerActorId}' slotId='{actorHandle.PlayerSlotId}' sem PlayerInput no prefab materializado.");
            }

            if (inputs.Length > 1)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: playerActorId='{actorHandle.PlayerActorId}' slotId='{actorHandle.PlayerSlotId}' possui multiplos PlayerInput sem endpoint explicito.");
            }

            PlayerInput resolved = inputs[0];
            if (resolved == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: playerActorId='{actorHandle.PlayerActorId}' slotId='{actorHandle.PlayerSlotId}' PlayerInput invalido.");
            }

            bool actionsRebound = false;
            if (!ReferenceEquals(resolved.actions, canonicalActionsAsset))
            {
                resolved.actions = canonicalActionsAsset;
                actionsRebound = true;
                DebugUtility.Log(typeof(PlayerInputBindingAdapter),
                    $"[OBS][PlayerInputBinding] event='PlayerInputActionsReboundToCanonical' playerActorId='{actorHandle.PlayerActorId}' playerSlotId='{actorHandle.PlayerSlotId}' playerInput='{resolved.name}' source='{requirement.Source}' reason='{requirement.Reason}'.",
                    DebugUtility.Colors.Info);
            }

            InputActionMap playerActionMap = resolved.actions?.FindActionMap(InputModesDefaults.PlayerActionMapName, throwIfNotFound: false);
            if (playerActionMap == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: playerActorId='{actorHandle.PlayerActorId}' slotId='{actorHandle.PlayerSlotId}' sem ActionMap '{InputModesDefaults.PlayerActionMapName}'.");
            }

            if (!string.Equals(resolved.defaultActionMap, InputModesDefaults.PlayerActionMapName, StringComparison.Ordinal))
            {
                resolved.defaultActionMap = InputModesDefaults.PlayerActionMapName;
            }

            if (resolved.currentActionMap == null || !string.Equals(resolved.currentActionMap.name, InputModesDefaults.PlayerActionMapName, StringComparison.Ordinal))
            {
                resolved.SwitchCurrentActionMap(InputModesDefaults.PlayerActionMapName);
            }

            PlayerInputSlotBinding slotBinding = resolved.GetComponent<PlayerInputSlotBinding>();
            if (slotBinding == null)
            {
                slotBinding = resolved.gameObject.AddComponent<PlayerInputSlotBinding>();
            }

            PlayerSlotId authoredSlotId = slotBinding.PlayerSlotId;
            if (authoredSlotId.IsValid && authoredSlotId != actorHandle.PlayerSlotId)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: slot binding conflita playerActorId='{actorHandle.PlayerActorId}' expectedSlotId='{actorHandle.PlayerSlotId}' authoredSlotId='{authoredSlotId}'.");
            }

            slotBinding.Initialize(actorHandle.PlayerSlotId, requirement.Source, requirement.Reason);

            if (!slotBinding.IsValid || slotBinding.PlayerSlotId != actorHandle.PlayerSlotId)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: PlayerInputSlotBinding invalido para playerActorId='{actorHandle.PlayerActorId}' slotId='{actorHandle.PlayerSlotId}'.");
            }

            return new PlayerInputResolution(resolved, actionsRebound);
        }

        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal) &&
                string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal) &&
                left.ActivityOrdinal == right.ActivityOrdinal &&
                left.EntrySequence == right.EntrySequence;
        }

        private readonly struct PlayerInputResolution
        {
            public PlayerInputResolution(PlayerInput playerInput, bool actionsReboundToCanonical)
            {
                PlayerInput = playerInput;
                ActionsReboundToCanonical = actionsReboundToCanonical;
            }

            public PlayerInput PlayerInput { get; }
            public bool ActionsReboundToCanonical { get; }
        }

    }
}
