using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Bridges.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameplayResetContext = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetContext;
using GameplayResetRequest = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetRequest;
using GameplayResetScope = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetScope;
using GameplayResetStructs = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetStructs;
using IActorRegistry = _ImmersiveGames.NewScripts.Infrastructure.Actors.IActorRegistry;
using LegacyActor = _ImmersiveGames.Scripts.ActorSystems.IActor;
using NewActor = _ImmersiveGames.NewScripts.Infrastructure.Actors.IActor;

namespace _ImmersiveGames.NewScripts.Infrastructure.World.Scopes.Players
{
    public sealed class PlayersResetParticipant : IResetScopeParticipant
    {
        private readonly PlayersResetPayloadBuilder _payloadBuilder = new();
        private readonly List<PlayerResetTarget> _playerTargets = new(8);
        private readonly List<ResetTargetPayload> _payloads = new(8);
        private readonly List<NewActor> _actorBuffer = new(16);
        private readonly List<LegacyActor> _legacyPlayerBuffer = new(16);

        private IActorRegistry _actorRegistry;
        private IPlayerDomain _playerDomain;
        private string _sceneName = string.Empty;
        private int _resetSerial;
        private bool _dependenciesResolved;

        public ResetScope Scope => ResetScope.Players;

        public int Order => 0;

        public async Task ResetAsync(ResetContext context)
        {
            EnsureDependencies();
            CollectPlayerTargets();

            var reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "WorldLifecycle/SoftReset"
                : context.Reason;

            var serial = ++_resetSerial;
            var request = BuildResetRequest(reason);

            if (_playerTargets.Count == 0)
            {
                DebugUtility.LogWarning(typeof(PlayersResetParticipant),
                    "[PlayersResetParticipant] Nenhum player identificado; o reset seguirá apenas com participantes de cena elegíveis.");
            }

            _payloads.Clear();
            _payloads.AddRange(_payloadBuilder.Build(_playerTargets, GetSceneLabel()));

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] ResetScope.Players start (reason={reason}, players={_playerTargets.Count}, payloads={_payloads.Count})");

            if (_payloads.Count == 0)
            {
                DebugUtility.LogWarning(typeof(PlayersResetParticipant),
                    "[PlayersResetParticipant] Nenhum participante encontrado para ResetScope.Players. Verifique registries/PlayerDomain.");
                return;
            }

            for (var i = 0; i < _payloads.Count; i++)
            {
                var payload = _payloads[i];
                await RunPayloadAsync(payload, request, serial, reason);
            }

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] ResetScope.Players end (reason={reason}, players={_playerTargets.Count}, payloads={_payloads.Count})");
        }

        private void EnsureDependencies()
        {
            if (_dependenciesResolved)
            {
                return;
            }

            _sceneName = string.IsNullOrWhiteSpace(_sceneName)
                ? SceneManager.GetActiveScene().name
                : _sceneName;

            var provider = DependencyManager.Provider;

            provider.TryGetForScene(_sceneName, out _actorRegistry);
            provider.TryGetForScene(_sceneName, out _playerDomain);

            _dependenciesResolved = true;
        }

        private void CollectPlayerTargets()
        {
            _playerTargets.Clear();

            if (TryCollectFromActorRegistry())
            {
                return;
            }

            TryCollectFromPlayerDomain();

            if (_playerTargets.Count > 1)
            {
                _playerTargets.Sort((left, right) =>
                    string.CompareOrdinal(left.ActorId, right.ActorId));
            }
        }

        private bool TryCollectFromActorRegistry()
        {
            if (_actorRegistry == null)
            {
                return false;
            }

            _actorBuffer.Clear();
            _actorRegistry.GetActors(_actorBuffer);

            if (_actorBuffer.Count == 0)
            {
                return false;
            }

            foreach (var actor in _actorBuffer)
            {
                if (actor == null)
                {
                    continue;
                }

                if (!IsPlayerActor(actor))
                {
                    continue;
                }

                var transform = actor.Transform;
                if (transform == null)
                {
                    continue;
                }

                var actorId = actor.ActorId ?? string.Empty;
                _playerTargets.Add(new PlayerResetTarget(actorId, transform));
            }

            return _playerTargets.Count > 0;
        }

        private void TryCollectFromPlayerDomain()
        {
            if (_playerDomain == null)
            {
                return;
            }

            var players = _playerDomain.Players;
            if (players == null || players.Count == 0)
            {
                return;
            }

            _legacyPlayerBuffer.Clear();
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player != null)
                {
                    _legacyPlayerBuffer.Add(player);
                }
            }

            _legacyPlayerBuffer.Sort((left, right) =>
                string.CompareOrdinal(left?.ActorId, right?.ActorId));

            foreach (var player in _legacyPlayerBuffer)
            {
                if (player == null)
                {
                    continue;
                }

                var transform = player.Transform;
                if (transform == null)
                {
                    continue;
                }

                var actorId = player.ActorId ?? string.Empty;
                _playerTargets.Add(new PlayerResetTarget(actorId, transform));
            }
        }

        private static bool IsPlayerActor(NewActor actor)
        {
            return actor is IPlayerActorMarker;
        }

        private GameplayResetRequest BuildResetRequest(string reason)
        {
            var actorIds = new List<string>(_playerTargets.Count);

            for (var i = 0; i < _playerTargets.Count; i++)
            {
                var id = _playerTargets[i].ActorId;
                if (!string.IsNullOrWhiteSpace(id) && !actorIds.Contains(id))
                {
                    actorIds.Add(id);
                }
            }

            return new GameplayResetRequest(GameplayResetScope.PlayersOnly, reason, actorIds);
        }

        private async Task RunPayloadAsync(
            ResetTargetPayload payload,
            GameplayResetRequest request,
            int serial,
            string reason)
        {
            var components = payload.Components;
            var label = payload.Label;
            var componentCount = components?.Count ?? 0;

            DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Payload start: {label} (sceneLevel={payload.IsSceneLevel}, components={componentCount})");

            if (componentCount == 0)
            {
                DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                    $"[PlayersResetParticipant] Payload skipped (no components): {label}");
                return;
            }

            await RunPhaseAsync(GameplayResetStructs.Cleanup, payload, request, serial, reason);
            await RunPhaseAsync(GameplayResetStructs.Restore, payload, request, serial, reason);
            await RunPhaseAsync(GameplayResetStructs.Rebind, payload, request, serial, reason);

            DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Payload end: {label} (components={componentCount})");
        }

        private async Task RunPhaseAsync(
            GameplayResetStructs phase,
            ResetTargetPayload payload,
            GameplayResetRequest request,
            int serial,
            string reason)
        {
            var ctx = CreateGameplayResetContext(request, phase, serial);
            var components = payload.Components;
            var failures = new List<string>();

            DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Phase start: {phase} | payload={payload.Label} | components={components.Count} | reason={reason}");

            for (var i = 0; i < components.Count; i++)
            {
                var entry = components[i];
                var component = entry.Component;

                if (component == null)
                {
                    DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                        $"[PlayersResetParticipant] Component nulo ignorado durante {phase} | payload={payload.Label} | source={entry.SourceLabel}");
                    continue;
                }

                try
                {
                    await InvokePhaseAsync(component, phase, ctx);
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError(typeof(PlayersResetParticipant),
                        $"[PlayersResetParticipant] Falha durante {phase} | payload={payload.Label} | source={entry.SourceLabel} | ex={ex}");
                    failures.Add($"{entry.SourceLabel}::{component.GetType().Name} => {ex.GetType().Name}");
                }
            }

            if (failures.Count > 0)
            {
                var summary = string.Join("; ", failures);
                DebugUtility.LogError(typeof(PlayersResetParticipant),
                    $"[PlayersResetParticipant] Falhas detectadas em {phase} | payload={payload.Label} | count={failures.Count} | sources={summary}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                throw new AggregateException($"PlayersResetParticipant failures in {phase} ({payload.Label})", new Exception(summary));
#endif
            }

            DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Phase end: {phase} | payload={payload.Label} | components={components.Count} | reason={reason}");
        }

        private GameplayResetContext CreateGameplayResetContext(
            GameplayResetRequest request,
            GameplayResetStructs phase,
            int serial)
        {
            var sceneName = GetSceneLabel();

            return new GameplayResetContext(
                sceneName,
                request,
                serial,
                Time.frameCount,
                phase);
        }

        private static Task InvokePhaseAsync(IResetInterfaces component, GameplayResetStructs phase, GameplayResetContext ctx)
        {
            return phase switch
            {
                GameplayResetStructs.Cleanup => component.Reset_CleanupAsync(ctx),
                GameplayResetStructs.Restore => component.Reset_RestoreAsync(ctx),
                GameplayResetStructs.Rebind => component.Reset_RebindAsync(ctx),
                _ => Task.CompletedTask
            };
        }

        private string GetSceneLabel()
        {
            return string.IsNullOrWhiteSpace(_sceneName)
                ? SceneManager.GetActiveScene().name
                : _sceneName;
        }
    }
}
