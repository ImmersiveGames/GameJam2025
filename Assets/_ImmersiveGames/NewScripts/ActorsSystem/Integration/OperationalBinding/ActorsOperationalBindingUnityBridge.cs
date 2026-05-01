using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
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
    public sealed class ActorsOperationalBindingUnityBridge : IDisposable, IActorsPendingBindingQueryPort
    {
        private readonly IActorsParticipantRuntimeMappingQueryPort _participantRuntimeMappingQueryPort;
        private readonly IActorsOperationalBindingInPort _bindingInPort;
        private readonly IActorsOperationalBindingQueryPort _bindingQueryPort;

        private readonly EventBinding<ParticipationSnapshotChangedEvent> _participationBinding;
        private readonly EventBinding<ActorsOperationalMaterializationCompletedEvent> _materializationCompletedBinding;
        private readonly EventBinding<ActorsParticipantRuntimeMappingUpdatedEvent> _mappingUpdatedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;

        private readonly Dictionary<int, ActorsUnityOperationalHandles> _pendingHandlesByPlayerInputInstanceId = new();
        private readonly Dictionary<RuntimeActorId, ActorsPendingBindingEntry> _pendingByRuntimeActorId = new(64);
        private ActorsPendingBindingSnapshot _pendingSnapshot = ActorsPendingBindingSnapshot.Empty;
        private string _activePendingScopeKey = string.Empty;
        private PlayerInputManager _currentPlayerInputManager;
        private bool _disposed;

        public ActorsPendingBindingSnapshot Current => _pendingSnapshot;

        public ActorsOperationalBindingUnityBridge(
            IActorsSemanticParticipationInPort semanticParticipationPort,
            IActorsParticipantRuntimeMappingQueryPort participantRuntimeMappingQueryPort,
            IActorsOperationalBindingInPort bindingInPort,
            IActorsOperationalBindingQueryPort bindingQueryPort)
        {
            _participantRuntimeMappingQueryPort = participantRuntimeMappingQueryPort ?? throw new ArgumentNullException(nameof(participantRuntimeMappingQueryPort));
            _bindingInPort = bindingInPort ?? throw new ArgumentNullException(nameof(bindingInPort));
            _bindingQueryPort = bindingQueryPort ?? throw new ArgumentNullException(nameof(bindingQueryPort));

            _participationBinding = new EventBinding<ParticipationSnapshotChangedEvent>(OnParticipationChanged);
            _materializationCompletedBinding = new EventBinding<ActorsOperationalMaterializationCompletedEvent>(OnMaterializationCompleted);
            _mappingUpdatedBinding = new EventBinding<ActorsParticipantRuntimeMappingUpdatedEvent>(OnMappingUpdated);
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);

            EventBus<ParticipationSnapshotChangedEvent>.Register(_participationBinding);
            EventBus<ActorsOperationalMaterializationCompletedEvent>.Register(_materializationCompletedBinding);
            EventBus<ActorsParticipantRuntimeMappingUpdatedEvent>.Register(_mappingUpdatedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);

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
            EventBus<ActorsParticipantRuntimeMappingUpdatedEvent>.Unregister(_mappingUpdatedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);

            UnbindCurrentPlayerInputManager();
            _pendingHandlesByPlayerInputInstanceId.Clear();
            _pendingByRuntimeActorId.Clear();
            _pendingSnapshot = ActorsPendingBindingSnapshot.Empty;
        }

        private void OnParticipationChanged(ParticipationSnapshotChangedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (evt.IsCleared)
            {
                if (evt.ClearKind == ParticipationSnapshotClearKind.PhaseSelection)
                {
                    DebugUtility.LogVerbose(typeof(ActorsOperationalBindingUnityBridge),
                        $"[OBS][ActorsSystem][OperationalBinding] Participation clear transitivo ignorado para preservar binding operacional entre phases. source='{evt.Source}' reason='{evt.Reason}' clearKind='{evt.ClearKind}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                _bindingInPort.Clear(evt.Reason);
                _pendingHandlesByPlayerInputInstanceId.Clear();
                _pendingByRuntimeActorId.Clear();
                _activePendingScopeKey = string.Empty;
                RebuildPendingSnapshot("participation-cleared");
                return;
            }

            if (!evt.IsValid)
            {
                return;
            }

            // Sem fallback participant->axis: binding depende de mapping canonico consolidado.
        }

        private void OnMaterializationCompleted(ActorsOperationalMaterializationCompletedEvent evt)
        {
            if (_disposed || string.IsNullOrWhiteSpace(evt.ActorId))
            {
                return;
            }

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

            if (!IsBindingEligible(evt, handles))
            {
                DebugUtility.LogVerbose(typeof(ActorsOperationalBindingUnityBridge),
                    $"[OBS][ActorsSystem][OperationalBinding] binding_skip runtimeActorId='{runtimeActorId}' actorSetRef='{AsText(evt.ActorSetRef)}' reason='non_bindable_actor_no_semantic_and_no_handles'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (evt.IsPreserveExisting && evt.HasSemanticParticipantId && !handles.HasAnyHandle)
            {
                if (_bindingQueryPort.TryGetByAxisActorId(evt.AxisActorId, out ActorsOperationalBindingEntry existing) && existing.IsValid)
                {
                    DebugUtility.Log(typeof(ActorsOperationalBindingUnityBridge),
                        $"[OBS][ActorsSystem][OperationalBinding] preserve_existing_confirmed runtimeActorId='{runtimeActorId}' participantId='{evt.SemanticParticipantId}' axisActorId='{evt.AxisActorId}' reason='preserve-existing-without-handles-binding-already-exists'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                PublishConflict(
                    ActorsBindingConflictCode.InvalidBindingState,
                    evt.SemanticParticipantId,
                    evt.AxisActorId,
                    runtimeActorId,
                    "GameplayRuntime/ActorsOperationalMaterializationHandoff",
                    "preserve-existing-without-handles-missing-binding");

                return;
            }

            string scopeKey = BuildScopeKey(evt.ActorSetRef, evt.ExecutionCycle.ToStampKey(), evt.ExecutionSignature);
            if (!TryEnterPendingScope(scopeKey, "GameplayRuntime/ActorsOperationalMaterializationHandoff", "materialization-context-switch"))
            {
                return;
            }

            MarkPendingReplacedForCanonicalIdentity(evt.SemanticParticipantId, evt.AxisActorId, runtimeActorId, scopeKey);
            UpsertPending(runtimeActorId, handles, evt.SemanticParticipantId, evt.AxisActorId, evt.ActorSetRef, scopeKey, "GameplayRuntime/ActorsOperationalMaterializationHandoff", "awaiting-mapping-after-materialization");

            TryBindFromMapping(runtimeActorId, "GameplayRuntime/ActorsOperationalMaterializationHandoff", "materialized-and-bound");
        }

        private void OnMappingUpdated(ActorsParticipantRuntimeMappingUpdatedEvent evt)
        {
            if (_disposed || !evt.IsValid)
            {
                return;
            }

            TryBindFromMapping(evt.Entry.RuntimeActorId, "ActorsSystem/ParticipantRuntimeMappingUpdated", "mapping-consolidated");
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (evt.context.RouteKind != SceneRouteKind.Gameplay)
            {
                ReconcilePendingMissingMappings("SceneFlow/SceneTransitionCompleted", "explicit-reconciliation-exit-non-gameplay");
                ClearPendingScope("scene-transition-non-gameplay");
                RebindPlayerInputManagerIfNeeded();
                return;
            }

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

                UpsertPending(runtimeActorId, handles, string.Empty, AxisActorId.None, string.Empty, _activePendingScopeKey, "Unity/PlayerInputManager", "awaiting-mapping-after-player-joined");
                TryBindFromMapping(runtimeActorId, "Unity/PlayerInputManager", "player-joined");

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
                if (_pendingByRuntimeActorId.Remove(runtimeActorId))
                {
                    RebuildPendingSnapshot("pending-removed-player-left");
                }
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

            var pending = new List<ActorsPendingBindingEntry>(_pendingByRuntimeActorId.Values);
            bool changed = false;
            for (int index = 0; index < pending.Count; index += 1)
            {
                if (!pending[index].UnityHandles.Equals(handles))
                {
                    continue;
                }

                changed |= _pendingByRuntimeActorId.Remove(pending[index].RuntimeActorId);
            }

            if (changed)
            {
                RebuildPendingSnapshot("pending-removed-player-left-handles");
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
                if (!_bindingInPort.TrySetState(updated.AxisActorId, ActorsOperationalBindingState.Active, "unity-player-input-active", source))
                {
                    PublishConflict(ActorsBindingConflictCode.InvalidBindingState, updated.ParticipantId, updated.AxisActorId, runtimeActorId, source, "failed_transition_to_active_after_runtime_bind");
                }
            }

            return true;
        }

        private bool TryBindFromMapping(RuntimeActorId runtimeActorId, string source, string reason)
        {
            if (!runtimeActorId.IsValid)
            {
                return false;
            }

            if (!_participantRuntimeMappingQueryPort.TryGetByRuntimeActorId(runtimeActorId, out ActorsParticipantRuntimeMappingEntry mapping))
            {
                TouchPending(runtimeActorId, source, "mapping-not-yet-available");
                return false;
            }

            _pendingByRuntimeActorId.TryGetValue(runtimeActorId, out ActorsPendingBindingEntry pending);
            ActorsUnityOperationalHandles pendingHandles = pending.IsValid ? pending.UnityHandles : ActorsUnityOperationalHandles.None;
            ActorsUnityOperationalHandles handles = pendingHandles.HasAnyHandle ? pendingHandles : ActorsUnityOperationalHandles.None;
            var entry = BuildEntry(mapping.ParticipantId, mapping.AxisActorId, runtimeActorId, handles, source, reason);
            if (!_bindingInPort.Upsert(entry))
            {
                return false;
            }

            if (entry.State == ActorsOperationalBindingState.Bound &&
                !_bindingInPort.TrySetState(mapping.AxisActorId, ActorsOperationalBindingState.Active, "unity-player-input-active", source))
            {
                PublishConflict(ActorsBindingConflictCode.InvalidBindingState, mapping.ParticipantId, mapping.AxisActorId, runtimeActorId, source, "failed_transition_to_active_after_mapping_bind");
            }

            if (entry.State != ActorsOperationalBindingState.Unbound)
            {
                if (_pendingByRuntimeActorId.Remove(runtimeActorId))
                {
                    RebuildPendingSnapshot("pending-resolved");
                }
            }

            return true;
        }

        public bool TryGetCurrent(out ActorsPendingBindingSnapshot snapshot)
        {
            snapshot = _pendingSnapshot;
            return snapshot.IsValid;
        }

        public bool TryGetByRuntimeActorId(RuntimeActorId runtimeActorId, out ActorsPendingBindingEntry entry)
        {
            entry = default;
            return runtimeActorId.IsValid && _pendingByRuntimeActorId.TryGetValue(runtimeActorId, out entry);
        }

        public bool TryGetAll(List<ActorsPendingBindingEntry> target)
        {
            if (target == null)
            {
                return false;
            }

            target.Clear();
            if (_pendingByRuntimeActorId.Count == 0)
            {
                return false;
            }

            var ordered = new List<ActorsPendingBindingEntry>(_pendingByRuntimeActorId.Values);
            ordered.Sort(static (left, right) => string.CompareOrdinal(left.RuntimeActorId.Value, right.RuntimeActorId.Value));
            target.AddRange(ordered);
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

        private void PublishConflict(
            ActorsBindingConflictCode code,
            string participantId,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            string source,
            string reason)
        {
            var conflict = new ActorsBindingConflict(code, participantId, axisActorId, runtimeActorId, source, reason);
            DebugUtility.LogError(typeof(ActorsOperationalBindingUnityBridge),
                $"[FATAL][ActorsSystem][OperationalBinding] conflict code='{conflict.Code}' participantId='{AsText(conflict.ParticipantId)}' axisActorId='{conflict.AxisActorId}' runtimeActorId='{conflict.RuntimeActorId}' source='{AsText(conflict.Source)}' reason='{AsText(conflict.Reason)}'.");
            EventBus<ActorsBindingConflictEvent>.Raise(new ActorsBindingConflictEvent(conflict));
        }

        private void UpsertPending(
            RuntimeActorId runtimeActorId,
            ActorsUnityOperationalHandles handles,
            string semanticParticipantId,
            AxisActorId axisActorId,
            string actorSetRef,
            string scopeKey,
            string source,
            string reason)
        {
            if (!runtimeActorId.IsValid)
            {
                return;
            }

            if (_pendingByRuntimeActorId.TryGetValue(runtimeActorId, out ActorsPendingBindingEntry current))
            {
                var updated = new ActorsPendingBindingEntry(
                    runtimeActorId,
                    handles.HasAnyHandle ? handles : current.UnityHandles,
                    string.IsNullOrWhiteSpace(semanticParticipantId) ? current.SemanticParticipantId : semanticParticipantId,
                    axisActorId.IsValid ? axisActorId : current.AxisActorId,
                    string.IsNullOrWhiteSpace(actorSetRef) ? current.ActorSetRef : actorSetRef,
                    string.IsNullOrWhiteSpace(scopeKey) ? current.ScopeKey : scopeKey,
                    source,
                    reason,
                    current.FirstSeenUtcTicks,
                    DateTime.UtcNow.Ticks,
                    current.Attempts);
                _pendingByRuntimeActorId[runtimeActorId] = updated;
            }
            else
            {
                _pendingByRuntimeActorId[runtimeActorId] = new ActorsPendingBindingEntry(
                    runtimeActorId,
                    handles,
                    semanticParticipantId,
                    axisActorId,
                    actorSetRef,
                    scopeKey,
                    source,
                    reason,
                    DateTime.UtcNow.Ticks,
                    DateTime.UtcNow.Ticks,
                    0);
            }

            DebugUtility.Log(typeof(ActorsOperationalBindingUnityBridge),
                $"[OBS][ActorsSystem][OperationalBinding] pending_binding runtimeActorId='{runtimeActorId}' source='{AsText(source)}' reason='{AsText(reason)}' hasHandles='{handles.HasAnyHandle.ToString().ToLowerInvariant()}'.",
                DebugUtility.Colors.Info);
            RebuildPendingSnapshot(reason);
        }

        private void TouchPending(RuntimeActorId runtimeActorId, string source, string reason)
        {
            if (!_pendingByRuntimeActorId.TryGetValue(runtimeActorId, out ActorsPendingBindingEntry current))
            {
                return;
            }

            _pendingByRuntimeActorId[runtimeActorId] = current.WithAttempt(source, reason, incrementAttempts: true);
            RebuildPendingSnapshot(reason);
        }

        private void ReconcilePendingMissingMappings(string source, string reason)
        {
            if (_pendingByRuntimeActorId.Count == 0)
            {
                return;
            }

            var pending = new List<ActorsPendingBindingEntry>(_pendingByRuntimeActorId.Values);
            for (int index = 0; index < pending.Count; index += 1)
            {
                ActorsPendingBindingEntry entry = pending[index];
                if (!string.IsNullOrWhiteSpace(_activePendingScopeKey) &&
                    !string.Equals(entry.ScopeKey, _activePendingScopeKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_participantRuntimeMappingQueryPort.TryGetByRuntimeActorId(entry.RuntimeActorId, out _))
                {
                    continue;
                }

                PublishConflict(
                    ActorsBindingConflictCode.MissingMapping,
                    string.IsNullOrWhiteSpace(entry.SemanticParticipantId) ? string.Empty : entry.SemanticParticipantId,
                    entry.AxisActorId.IsValid ? entry.AxisActorId : AxisActorId.None,
                    entry.RuntimeActorId,
                    source,
                    reason);

                TouchPending(entry.RuntimeActorId, source, "reconciled-missing-mapping");
            }
        }

        private void MarkPendingReplacedForCanonicalIdentity(
            string semanticParticipantId,
            AxisActorId axisActorId,
            RuntimeActorId newRuntimeActorId,
            string scopeKey)
        {
            if (string.IsNullOrWhiteSpace(semanticParticipantId) || !axisActorId.IsValid || !newRuntimeActorId.IsValid)
            {
                return;
            }

            var pending = new List<ActorsPendingBindingEntry>(_pendingByRuntimeActorId.Values);
            bool changed = false;
            for (int index = 0; index < pending.Count; index += 1)
            {
                ActorsPendingBindingEntry current = pending[index];
                if (current.RuntimeActorId == newRuntimeActorId)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(scopeKey) &&
                    !string.Equals(current.ScopeKey, scopeKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(current.SemanticParticipantId, semanticParticipantId, StringComparison.Ordinal) ||
                    current.AxisActorId != axisActorId)
                {
                    continue;
                }

                changed |= _pendingByRuntimeActorId.Remove(current.RuntimeActorId);
                DebugUtility.LogVerbose(typeof(ActorsOperationalBindingUnityBridge),
                    $"[OBS][ActorsSystem][OperationalBinding] pending_replaced oldRuntimeActorId='{current.RuntimeActorId}' newRuntimeActorId='{newRuntimeActorId}' participantId='{semanticParticipantId}' axisActorId='{axisActorId}' scopeKey='{AsText(scopeKey)}'.",
                    DebugUtility.Colors.Info);
            }

            if (changed)
            {
                RebuildPendingSnapshot("pending-replaced-canonical-identity");
            }
        }

        private void RebuildPendingSnapshot(string reason)
        {
            if (_pendingByRuntimeActorId.Count == 0)
            {
                _pendingSnapshot = new ActorsPendingBindingSnapshot(
                    "actors-pending-binding|count:0",
                    Array.Empty<ActorsPendingBindingEntry>(),
                    string.IsNullOrWhiteSpace(reason) ? "updated" : reason.Trim());
                EventBus<ActorsPendingBindingSnapshotChangedEvent>.Raise(new ActorsPendingBindingSnapshotChangedEvent(_pendingSnapshot));
                return;
            }

            var entries = new List<ActorsPendingBindingEntry>(_pendingByRuntimeActorId.Values);
            entries.Sort(static (left, right) => string.CompareOrdinal(left.RuntimeActorId.Value, right.RuntimeActorId.Value));

            var signature = new System.Text.StringBuilder(128);
            signature.Append("actors-pending-binding|count:");
            signature.Append(entries.Count);
            for (int index = 0; index < entries.Count; index += 1)
            {
                signature.Append("|r:");
                signature.Append(entries[index].RuntimeActorId.Value);
                signature.Append("|s:");
                signature.Append(string.IsNullOrWhiteSpace(entries[index].ScopeKey) ? "<none>" : entries[index].ScopeKey);
                signature.Append("|a:");
                signature.Append(entries[index].Attempts);
            }

            _pendingSnapshot = new ActorsPendingBindingSnapshot(
                signature.ToString(),
                entries.ToArray(),
                string.IsNullOrWhiteSpace(reason) ? "updated" : reason.Trim());

            EventBus<ActorsPendingBindingSnapshotChangedEvent>.Raise(new ActorsPendingBindingSnapshotChangedEvent(_pendingSnapshot));
        }

        private bool IsBindingEligible(ActorsOperationalMaterializationCompletedEvent evt, ActorsUnityOperationalHandles handles)
        {
            return evt.HasSemanticParticipantId || handles.HasAnyHandle;
        }

        private bool TryEnterPendingScope(string nextScopeKey, string source, string reason)
        {
            if (string.IsNullOrWhiteSpace(nextScopeKey))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(_activePendingScopeKey))
            {
                _activePendingScopeKey = nextScopeKey;
                return true;
            }

            if (string.Equals(_activePendingScopeKey, nextScopeKey, StringComparison.Ordinal))
            {
                return true;
            }

            ReconcilePendingMissingMappings(source, reason);
            ClearPendingScope("pending-scope-context-switch");
            _activePendingScopeKey = nextScopeKey;
            return true;
        }

        private void ClearPendingScope(string reason)
        {
            _pendingByRuntimeActorId.Clear();
            _activePendingScopeKey = string.Empty;
            RebuildPendingSnapshot(reason);
        }

        private static string BuildScopeKey(string actorSetRef, string cycleStamp, string executionSignature)
        {
            string normalizedActorSetRef = string.IsNullOrWhiteSpace(actorSetRef) ? "<none>" : actorSetRef.Trim();
            string normalizedCycle = string.IsNullOrWhiteSpace(cycleStamp) ? "<none>" : cycleStamp.Trim();
            string normalizedExecution = string.IsNullOrWhiteSpace(executionSignature) ? "<none>" : executionSignature.Trim();
            return $"actorSetRef:{normalizedActorSetRef}|cycle:{normalizedCycle}|execution:{normalizedExecution}";
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }

    public readonly struct ActorsPendingBindingSnapshotChangedEvent : IEvent
    {
        public ActorsPendingBindingSnapshotChangedEvent(ActorsPendingBindingSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public ActorsPendingBindingSnapshot Snapshot { get; }
        public bool IsValid => Snapshot.IsValid;
    }
}
