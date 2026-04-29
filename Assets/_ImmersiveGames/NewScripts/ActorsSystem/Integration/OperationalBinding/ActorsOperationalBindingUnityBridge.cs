using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.OperationalBinding
{
    /// <summary>
    /// Runtime bridge that translates Unity operational join/pairing events into Actors Operational Binding updates.
    /// Unity operational IDs remain operational handles and never become semantic identifiers.
    /// </summary>
    public sealed class ActorsOperationalBindingUnityBridge : IDisposable
    {
        private readonly IActorsSemanticParticipationInPort _semanticParticipationPort;
        private readonly IActorsParticipantRuntimeMappingQueryPort _participantRuntimeMappingQueryPort;
        private readonly IActorsOperationalBindingInPort _bindingInPort;
        private readonly IActorsOperationalBindingQueryPort _bindingQueryPort;

        private readonly EventBinding<ParticipationSnapshotChangedEvent> _participationBinding;
        private readonly EventBinding<ActorsOperationalMaterializationCompletedEvent> _materializationCompletedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;

        private readonly Dictionary<int, ActorsUnityOperationalHandles> _pendingHandlesByPlayerInputInstanceId = new();
        private PlayerInputManager _currentPlayerInputManager;
        private bool _disposed;

        public ActorsOperationalBindingUnityBridge(
            IActorsSemanticParticipationInPort semanticParticipationPort,
            IActorsParticipantRuntimeMappingQueryPort participantRuntimeMappingQueryPort,
            IActorsOperationalBindingInPort bindingInPort,
            IActorsOperationalBindingQueryPort bindingQueryPort)
        {
            _semanticParticipationPort = semanticParticipationPort ?? throw new ArgumentNullException(nameof(semanticParticipationPort));
            _participantRuntimeMappingQueryPort = participantRuntimeMappingQueryPort ?? throw new ArgumentNullException(nameof(participantRuntimeMappingQueryPort));
            _bindingInPort = bindingInPort ?? throw new ArgumentNullException(nameof(bindingInPort));
            _bindingQueryPort = bindingQueryPort ?? throw new ArgumentNullException(nameof(bindingQueryPort));

            _participationBinding = new EventBinding<ParticipationSnapshotChangedEvent>(OnParticipationChanged);
            _materializationCompletedBinding = new EventBinding<ActorsOperationalMaterializationCompletedEvent>(OnMaterializationCompleted);
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);

            EventBus<ParticipationSnapshotChangedEvent>.Register(_participationBinding);
            EventBus<ActorsOperationalMaterializationCompletedEvent>.Register(_materializationCompletedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);

            SeedSemanticParticipants("ActorsOperationalBindingUnityBridge/Bootstrap");
            RebindPlayerInputManagerIfNeeded();

            DebugUtility.Log(typeof(ActorsOperationalBindingUnityBridge),
                "[OBS][ActorsSystem][OperationalBinding] Unity bridge registrado (PlayerInput/PlayerInputManager -> OperationalBindingInPort).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            EventBus<ParticipationSnapshotChangedEvent>.Unregister(_participationBinding);
            EventBus<ActorsOperationalMaterializationCompletedEvent>.Unregister(_materializationCompletedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);

            UnbindCurrentPlayerInputManager();
            _pendingHandlesByPlayerInputInstanceId.Clear();
        }

        private void OnParticipationChanged(ParticipationSnapshotChangedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (evt.IsCleared)
            {
                if (IsPhaseSelectionClear(evt))
                {
                    DebugUtility.LogVerbose(typeof(ActorsOperationalBindingUnityBridge),
                        $"[OBS][ActorsSystem][OperationalBinding] Participation clear transitivo ignorado para preservar binding operacional entre phases. source='{evt.Source}' reason='{evt.Reason}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                _bindingInPort.Clear(evt.Reason);
                _pendingHandlesByPlayerInputInstanceId.Clear();
                return;
            }

            if (!evt.IsValid)
            {
                return;
            }

            SeedSemanticParticipants("SessionFlow/ParticipationSnapshotChanged");
        }

        private void OnMaterializationCompleted(ActorsOperationalMaterializationCompletedEvent evt)
        {
            if (_disposed || string.IsNullOrWhiteSpace(evt.ActorId) || evt.ActorKind != ActorKind.Player)
            {
                return;
            }

            SeedSemanticParticipants("GameplayRuntime/ActorsOperationalMaterializationHandoff");

            RuntimeActorId runtimeActorId = new RuntimeActorId(evt.ActorId);
            if (!runtimeActorId.IsValid)
            {
                return;
            }

            IActor actor = evt.Actor;
            PlayerInput playerInput = ResolvePlayerInput(actor);
            ActorsUnityOperationalHandles handles = ResolveUnityHandles(playerInput);
            if (playerInput != null)
            {
                _pendingHandlesByPlayerInputInstanceId.Remove(playerInput.GetInstanceID());
            }

            if (_participantRuntimeMappingQueryPort.TryGetByRuntimeActorId(runtimeActorId, out ActorsParticipantRuntimeMappingEntry mapping))
            {
                var entry = BuildEntry(
                    mapping.ParticipantId,
                    mapping.AxisActorId,
                    runtimeActorId,
                    handles,
                    source: "GameplayRuntime/ActorsOperationalMaterializationHandoff",
                    reason: "materialized-and-bound");

                if (_bindingInPort.Upsert(entry) && entry.State == ActorsOperationalBindingState.Bound)
                {
                    _bindingInPort.TrySetState(mapping.AxisActorId, ActorsOperationalBindingState.Active, "unity-player-input-active", "GameplayRuntime/ActorsOperationalMaterializationHandoff");
                }

                return;
            }

            // Fallback autoritativo para o mesmo evento de materializacao (evita dependencia de ordem do EventBus/HashSet).
            if (evt.HasSemanticParticipantId)
            {
                string participantId = evt.SemanticParticipantId.Trim();
                AxisActorId axisActorId = AxisActorId.FromParticipantId(participantId);
                if (!axisActorId.IsValid)
                {
                    return;
                }

                var entry = BuildEntry(
                    participantId,
                    axisActorId,
                    runtimeActorId,
                    handles,
                    source: "GameplayRuntime/ActorsOperationalMaterializationHandoff",
                    reason: "materialized-and-bound");

                if (_bindingInPort.Upsert(entry) && entry.State == ActorsOperationalBindingState.Bound)
                {
                    _bindingInPort.TrySetState(axisActorId, ActorsOperationalBindingState.Active, "unity-player-input-active", "GameplayRuntime/ActorsOperationalMaterializationHandoff");
                }
            }
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            _ = evt;
            RebindPlayerInputManagerIfNeeded();
        }

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            if (_disposed || playerInput == null)
            {
                return;
            }

            ActorsUnityOperationalHandles handles = ResolveUnityHandles(playerInput);
            if (!handles.HasAnyHandle)
            {
                return;
            }

            if (TryResolveRuntimeActorId(playerInput, out RuntimeActorId runtimeActorId))
            {
                if (TryUpsertMaterializedEntry(runtimeActorId, handles, "Unity/PlayerInputManager", "player-joined"))
                {
                    return;
                }

                if (_participantRuntimeMappingQueryPort.TryGetByRuntimeActorId(runtimeActorId, out ActorsParticipantRuntimeMappingEntry mapping))
                {
                    var entry = BuildEntry(
                        mapping.ParticipantId,
                        mapping.AxisActorId,
                        runtimeActorId,
                        handles,
                        source: "Unity/PlayerInputManager",
                        reason: "player-joined");

                    if (_bindingInPort.Upsert(entry) && entry.State == ActorsOperationalBindingState.Bound)
                    {
                        _bindingInPort.TrySetState(mapping.AxisActorId, ActorsOperationalBindingState.Active, "unity-player-input-active", "Unity/PlayerInputManager");
                    }
                }

                return;
            }

            _pendingHandlesByPlayerInputInstanceId[playerInput.GetInstanceID()] = handles;
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            if (_disposed || playerInput == null)
            {
                return;
            }

            _pendingHandlesByPlayerInputInstanceId.Remove(playerInput.GetInstanceID());

            if (TryResolveRuntimeActorId(playerInput, out RuntimeActorId runtimeActorId) &&
                _bindingQueryPort.TryGetByRuntimeActorId(runtimeActorId, out ActorsOperationalBindingEntry runtimeEntry))
            {
                _bindingInPort.TrySetState(runtimeEntry.AxisActorId, ActorsOperationalBindingState.Disconnected, "player-left", "Unity/PlayerInputManager");
                return;
            }

            ActorsUnityOperationalHandles handles = ResolveUnityHandles(playerInput);
            if (!handles.HasAnyHandle)
            {
                return;
            }

            var entries = new List<ActorsOperationalBindingEntry>(32);
            if (!_bindingQueryPort.TryGetAll(entries))
            {
                return;
            }

            for (int index = 0; index < entries.Count; index += 1)
            {
                ActorsOperationalBindingEntry entry = entries[index];
                if (entry.UnityHandles.Equals(handles))
                {
                    _bindingInPort.TrySetState(entry.AxisActorId, ActorsOperationalBindingState.Disconnected, "player-left", "Unity/PlayerInputManager");
                }
            }
        }

        private void SeedSemanticParticipants(string source)
        {
            if (!_semanticParticipationPort.TryGetCurrent(out ActorsSemanticParticipationSnapshot semanticSnapshot) || !semanticSnapshot.IsValid)
            {
                return;
            }

            ActorsSemanticParticipantRecord[] participants = semanticSnapshot.Participants ?? Array.Empty<ActorsSemanticParticipantRecord>();
            for (int index = 0; index < participants.Length; index += 1)
            {
                ActorsSemanticParticipantRecord participant = participants[index];
                if (!participant.IsValid || participant.ExpectedRole != ActorRole.Player)
                {
                    continue;
                }

                AxisActorId axisActorId = AxisActorId.FromParticipantId(participant.ParticipantId);
                if (!axisActorId.IsValid)
                {
                    continue;
                }

                if (_bindingQueryPort.TryGetByAxisActorId(axisActorId, out _))
                {
                    continue;
                }

                var entry = new ActorsOperationalBindingEntry(
                    participant.ParticipantId,
                    axisActorId,
                    RuntimeActorId.None,
                    ActorsUnityOperationalHandles.None,
                    ActorsOperationalBindingFlowStep.SemanticReady,
                    ActorsOperationalBindingState.Unbound,
                    source,
                    "semantic-ready");

                _bindingInPort.Upsert(entry);
            }
        }

        private bool TryUpsertMaterializedEntry(
            RuntimeActorId runtimeActorId,
            ActorsUnityOperationalHandles handles,
            string source,
            string reason)
        {
            if (!_bindingQueryPort.TryGetByRuntimeActorId(runtimeActorId, out ActorsOperationalBindingEntry current))
            {
                return false;
            }

            ActorsOperationalBindingFlowStep flowStep = handles.HasAnyHandle
                ? ActorsOperationalBindingFlowStep.UnityOperationalBound
                : ActorsOperationalBindingFlowStep.RuntimeMaterialized;
            ActorsOperationalBindingState state = handles.HasAnyHandle
                ? ActorsOperationalBindingState.Bound
                : ActorsOperationalBindingState.Unbound;

            var updated = new ActorsOperationalBindingEntry(
                current.ParticipantId,
                current.AxisActorId,
                runtimeActorId,
                handles.HasAnyHandle ? handles : current.UnityHandles,
                flowStep,
                state,
                source,
                reason);

            if (!_bindingInPort.Upsert(updated))
            {
                return false;
            }

            if (updated.State == ActorsOperationalBindingState.Bound)
            {
                _bindingInPort.TrySetState(updated.AxisActorId, ActorsOperationalBindingState.Active, "unity-player-input-active", source);
            }

            return true;
        }

        private ActorsOperationalBindingEntry BuildEntry(
            string participantId,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            ActorsUnityOperationalHandles handles,
            string source,
            string reason)
        {
            bool hasRuntime = runtimeActorId.IsValid;
            bool hasHandles = handles.HasAnyHandle;

            ActorsOperationalBindingFlowStep flowStep = ActorsOperationalBindingFlowStep.SemanticReady;
            ActorsOperationalBindingState state = ActorsOperationalBindingState.Unbound;

            if (hasRuntime)
            {
                flowStep = ActorsOperationalBindingFlowStep.RuntimeMaterialized;
            }

            if (hasRuntime && hasHandles)
            {
                flowStep = ActorsOperationalBindingFlowStep.UnityOperationalBound;
                state = ActorsOperationalBindingState.Bound;
            }

            return new ActorsOperationalBindingEntry(
                participantId,
                axisActorId,
                runtimeActorId,
                handles,
                flowStep,
                state,
                source,
                reason);
        }

        private static PlayerInput ResolvePlayerInput(IActor actor)
        {
            if (actor == null || actor.Transform == null)
            {
                return null;
            }

            return actor.Transform.GetComponent<PlayerInput>();
        }

        private static bool TryResolveRuntimeActorId(PlayerInput playerInput, out RuntimeActorId runtimeActorId)
        {
            runtimeActorId = RuntimeActorId.None;
            if (playerInput == null)
            {
                return false;
            }

            Component[] components = playerInput.GetComponents<Component>();
            if (components == null || components.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < components.Length; index += 1)
            {
                if (components[index] is not IActor actor || string.IsNullOrWhiteSpace(actor.ActorId))
                {
                    continue;
                }

                runtimeActorId = new RuntimeActorId(actor.ActorId);
                return runtimeActorId.IsValid;
            }

            return false;
        }

        private ActorsUnityOperationalHandles ResolveUnityHandles(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                return ActorsUnityOperationalHandles.None;
            }

            if (_pendingHandlesByPlayerInputInstanceId.TryGetValue(playerInput.GetInstanceID(), out ActorsUnityOperationalHandles pending))
            {
                return pending;
            }

            int playerIndex = playerInput.playerIndex;
            ulong inputUserId = (ulong)playerInput.user.id;

            return new ActorsUnityOperationalHandles(
                playerIndex >= 0 ? playerIndex : null,
                inputUserId > 0UL ? inputUserId : null);
        }

        private void RebindPlayerInputManagerIfNeeded()
        {
            PlayerInputManager manager = PlayerInputManager.instance;
            if (ReferenceEquals(manager, _currentPlayerInputManager))
            {
                return;
            }

            UnbindCurrentPlayerInputManager();
            _currentPlayerInputManager = manager;

            if (_currentPlayerInputManager == null)
            {
                return;
            }

            _currentPlayerInputManager.onPlayerJoined += OnPlayerJoined;
            _currentPlayerInputManager.onPlayerLeft += OnPlayerLeft;

            DebugUtility.LogVerbose(typeof(ActorsOperationalBindingUnityBridge),
                "[OBS][ActorsSystem][OperationalBinding] PlayerInputManager vinculado ao bridge operacional.",
                DebugUtility.Colors.Info);
        }

        private void UnbindCurrentPlayerInputManager()
        {
            if (_currentPlayerInputManager == null)
            {
                return;
            }

            _currentPlayerInputManager.onPlayerJoined -= OnPlayerJoined;
            _currentPlayerInputManager.onPlayerLeft -= OnPlayerLeft;
            _currentPlayerInputManager = null;
        }

        private static bool IsPhaseSelectionClear(ParticipationSnapshotChangedEvent evt)
        {
            return string.Equals(evt.Reason, "phase_selected", StringComparison.Ordinal);
        }
    }
}
