using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class PlayerInputBindingAdapter : IPlayerInputBindingAdapter
    {
        private readonly InputActionAsset _canonicalActionsAsset;

        public PlayerInputBindingAdapter(InputActionAsset canonicalActionsAsset)
        {
            _canonicalActionsAsset = canonicalActionsAsset ?? throw new ArgumentNullException(nameof(canonicalActionsAsset));
        }

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

                if (!registry.TryGetActiveHandleByParticipant(requirement.ParticipantId, out PlayerActorRuntimeHandle actorHandle) || !actorHandle.IsValid)
                {
                    throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' actor handle not found.");
                }
                PlayerActorId playerActorId = actorHandle.PlayerActorId;
                GameObject actorInstance = actorHandle.Instance;
                if (actorHandle.PlayerSlotId != requirement.PlayerSlotId || actorHandle.ActorId != requirement.ActorId)
                {
                    throw new InvalidOperationException($"stale_or_foreign_player_input_binding_requirement: handle mismatch participantId='{requirement.ParticipantId}' actorId='{requirement.ActorId}' playerSlotId='{requirement.PlayerSlotId}'.");
                }

                PlayerInputResolution resolution = ResolvePlayerInputFromActorOrFail(actorInstance, requirement, _canonicalActionsAsset);
                PlayerInput resolvedInput = resolution.PlayerInput;

                ActorCapabilitySurface capabilitySurface = actorHandle.CapabilitySurface ?? throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' missing ActorCapabilitySurface.");
                IActorCommandSourceHub commandHub = capabilitySurface.ActorCommandSourceHub ?? throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' missing Actor command input hub.");

                ApplyCurrentInputModeToPlayerInputOrFail(resolvedInput, requirement);

                PlayerActorInputBindingState bindingState = actorInstance.GetComponent<PlayerActorInputBindingState>();
                if (bindingState == null)
                {
                    bindingState = actorInstance.AddComponent<PlayerActorInputBindingState>();
                }

                commandHub.PrepareInputBindings(
                    resolvedInput,
                    $"PlayerInputBindingAdapter|source={requirement.Source}|reason={requirement.Reason}");

                bindingState.Bind(
                    activeIdentity,
                    requirement.PlayerSlotId,
                    playerActorId,
                    resolvedInput.GetInstanceID(),
                    resolvedInput.playerIndex,
                    resolvedInput.name);

                records.Add(new PlayerInputBindingRecord(
                    requirement,
                    actorHandle.ActorIdentity,
                    bound: true,
                    observedInputId: $"{resolvedInput.name}|index={resolvedInput.playerIndex}|instance={resolvedInput.GetInstanceID()}|actionsRebound={resolution.ActionsReboundToCanonical}|currentActionMap={resolvedInput.currentActionMap?.name ?? "<none>"}"));
            }

            return records;
        }

        private static PlayerInputResolution ResolvePlayerInputFromActorOrFail(
            GameObject actorInstance,
            PlayerInputBindingRequirement requirement,
            InputActionAsset canonicalActionsAsset)
        {
            if (actorInstance == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: actor instance missing for actorId='{requirement.ActorId}'.");
            }

            PlayerInput[] inputs = actorInstance.GetComponentsInChildren<PlayerInput>(includeInactive: true);
            if (inputs == null || inputs.Length == 0)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' slotId='{requirement.PlayerSlotId}' no PlayerInput found in materialized prefab.");
            }

            if (inputs.Length > 1)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' slotId='{requirement.PlayerSlotId}' multiple PlayerInput components without explicit endpoint.");
            }

            PlayerInput resolved = inputs[0];
            if (resolved == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' slotId='{requirement.PlayerSlotId}' invalid PlayerInput.");
            }

            bool actionsRebound = false;
            if (!ReferenceEquals(resolved.actions, canonicalActionsAsset))
            {
                resolved.actions = canonicalActionsAsset;
                actionsRebound = true;
                DebugUtility.Log(typeof(PlayerInputBindingAdapter),
                    $"[OBS][PlayerInputBinding] event='PlayerInputActionsReboundToCanonical' actorId='{requirement.ActorId}' playerSlotId='{requirement.PlayerSlotId}' playerInput='{resolved.name}' source='{requirement.Source}' reason='{requirement.Reason}'.",
                    DebugUtility.Colors.Info);
            }

            if (resolved.actions == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' slotId='{requirement.PlayerSlotId}' missing actions asset.");
            }

            PlayerInputSlotBinding slotBinding = resolved.GetComponent<PlayerInputSlotBinding>();
            if (slotBinding == null)
            {
                slotBinding = resolved.gameObject.AddComponent<PlayerInputSlotBinding>();
            }

            PlayerSlotId authoredSlotId = slotBinding.PlayerSlotId;
            if (authoredSlotId.IsValid && authoredSlotId != requirement.PlayerSlotId)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: slot binding conflict actorId='{requirement.ActorId}' expectedSlotId='{requirement.PlayerSlotId}' authoredSlotId='{authoredSlotId}'.");
            }

            slotBinding.Initialize(requirement.PlayerSlotId, requirement.Source, requirement.Reason);

            if (!slotBinding.IsValid || slotBinding.PlayerSlotId != requirement.PlayerSlotId)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: PlayerInputSlotBinding invalid for actorId='{requirement.ActorId}' slotId='{requirement.PlayerSlotId}'.");
            }

            return new PlayerInputResolution(resolved, actionsRebound);
        }

        private static void ApplyCurrentInputModeToPlayerInputOrFail(
            PlayerInput playerInput,
            PlayerInputBindingRequirement requirement)
        {
            if (playerInput == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' slotId='{requirement.PlayerSlotId}' missing PlayerInput before InputModes refresh.");
            }

            if (!DependencyManager.HasInstance ||
                !DependencyManager.Provider.TryGetGlobal<IInputModeService>(out IInputModeService inputModeService) ||
                inputModeService == null)
            {
                throw new InvalidOperationException($"PlayerInput binding failed: actorId='{requirement.ActorId}' slotId='{requirement.PlayerSlotId}' missing IInputModeService for canonical action map ownership.");
            }

            inputModeService.ApplyCurrentModeToPlayerInput(
                playerInput,
                $"PlayerInputBindingAdapter|source={requirement.Source}|reason={requirement.Reason}|apply_current_input_mode");
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
