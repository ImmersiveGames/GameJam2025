using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Detections;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Interactions;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Movement;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Shooting;
using _ImmersiveGames.Scripts.Utils.CameraSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameplayResetContext = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetContext;
using GameplayResetRequest = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetRequest;
using GameplayResetScope = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetScope;
using GameplayResetStructs = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetStructs;
using LegacyActor = _ImmersiveGames.Scripts.ActorSystems.IActor;
using NewActor = _ImmersiveGames.NewScripts.Infrastructure.Actors.IActor;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    public sealed class PlayersResetParticipant : IResetScopeParticipant
    {
        private readonly List<PlayerTarget> _playerTargets = new(8);
        private readonly List<NewActor> _actorBuffer = new(16);
        private readonly List<LegacyActor> _legacyPlayerBuffer = new(16);
        private readonly List<CanvasCameraBinder> _canvasBinders = new(8);

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

            var reason = context.Reason ?? "WorldLifecycle/SoftReset";
            var serial = ++_resetSerial;
            var request = BuildResetRequest(reason);

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] ResetScope.Players start (reason={reason}, players={_playerTargets.Count})");

            foreach (var target in _playerTargets)
            {
                var components = ResolvePlayerComponents(target);
                await RunPhaseAsync(target, components, GameplayResetStructs.Cleanup, request, serial);
                await RunPhaseAsync(target, components, GameplayResetStructs.Restore, request, serial);
                await RunPhaseAsync(target, components, GameplayResetStructs.Rebind, request, serial);
            }

            await RunCanvasCameraBinderPhasesAsync(request, serial);

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] ResetScope.Players end (reason={reason}, players={_playerTargets.Count})");
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

            provider.TryGetForScene<IActorRegistry>(_sceneName, out _actorRegistry);
            provider.TryGetForScene<IPlayerDomain>(_sceneName, out _playerDomain);

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
                _playerTargets.Add(new PlayerTarget(actorId, transform.gameObject, transform));
            }

            return _playerTargets.Count > 0;
        }

        private bool TryCollectFromPlayerDomain()
        {
            if (_playerDomain == null)
            {
                return false;
            }

            var players = _playerDomain.Players;
            if (players == null || players.Count == 0)
            {
                return false;
            }

            _legacyPlayerBuffer.Clear();
            _legacyPlayerBuffer.AddRange(players.Where(p => p != null));
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
                _playerTargets.Add(new PlayerTarget(actorId, transform.gameObject, transform));
            }

            return _playerTargets.Count > 0;
        }

        private static bool IsPlayerActor(NewActor actor)
        {
            return actor is PlayerActor or PlayerActorAdapter;
        }

        private GameplayResetRequest BuildResetRequest(string reason)
        {
            var actorIds = _playerTargets
                .Select(target => target.ActorId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return new GameplayResetRequest(GameplayResetScope.PlayersOnly, reason, actorIds);
        }

        private async Task RunPhaseAsync(
            PlayerTarget target,
            PlayerComponents components,
            GameplayResetStructs phase,
            GameplayResetRequest request,
            int serial)
        {
            var ctx = CreateGameplayResetContext(request, phase, serial);
            var actorId = string.IsNullOrWhiteSpace(target.ActorId) ? "<unknown>" : target.ActorId;
            var goName = target.Root != null ? target.Root.name : "<null>";
            var phaseLabel = phase.ToString();

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] {phaseLabel} start (actorId={actorId}, go={goName})");

            await InvokePhaseAsync(components.Movement, phase, ctx);
            await InvokePhaseAsync(components.Shoot, phase, ctx);
            await InvokePhaseAsync(components.Interact, phase, ctx);
            await InvokePhaseAsync(components.Detection, phase, ctx);

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] {phaseLabel} end (actorId={actorId}, go={goName})");
        }

        private GameplayResetContext CreateGameplayResetContext(
            GameplayResetRequest request,
            GameplayResetStructs phase,
            int serial)
        {
            var sceneName = !string.IsNullOrWhiteSpace(_sceneName)
                ? _sceneName
                : SceneManager.GetActiveScene().name;

            return new GameplayResetContext(
                sceneName,
                request,
                serial,
                Time.frameCount,
                phase);
        }

        private PlayerComponents ResolvePlayerComponents(PlayerTarget target)
        {
            var actorId = string.IsNullOrWhiteSpace(target.ActorId) ? "<unknown>" : target.ActorId;

            return new PlayerComponents(
                TryFindComponent<PlayerMovementController>(target.Root, actorId),
                TryFindComponent<PlayerShootController>(target.Root, actorId),
                TryFindComponent<PlayerInteractController>(target.Root, actorId),
                TryFindComponent<PlayerDetectionController>(target.Root, actorId));
        }

        private T TryFindComponent<T>(GameObject root, string actorId) where T : class
        {
            if (root == null)
            {
                return null;
            }

            var component = root.GetComponentInChildren<T>(true);
            if (component == null)
            {
                DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                    $"[PlayersResetParticipant] Component missing (actorId={actorId}, type={typeof(T).Name})");
            }

            return component;
        }

        private static Task InvokePhaseAsync(IResetInterfaces component, GameplayResetStructs phase, GameplayResetContext ctx)
        {
            if (component == null)
            {
                return Task.CompletedTask;
            }

            return phase switch
            {
                GameplayResetStructs.Cleanup => component.Reset_CleanupAsync(ctx),
                GameplayResetStructs.Restore => component.Reset_RestoreAsync(ctx),
                GameplayResetStructs.Rebind => component.Reset_RebindAsync(ctx),
                _ => Task.CompletedTask
            };
        }

        private async Task RunCanvasCameraBinderPhasesAsync(GameplayResetRequest request, int serial)
        {
            _canvasBinders.Clear();
            _canvasBinders.AddRange(FindObjectsOfType<CanvasCameraBinder>(includeInactive: false));

            if (!string.IsNullOrWhiteSpace(_sceneName))
            {
                _canvasBinders.RemoveAll(binder => binder == null || binder.gameObject.scene.name != _sceneName);
            }
            else
            {
                _canvasBinders.RemoveAll(binder => binder == null);
            }

            if (_canvasBinders.Count > 1)
            {
                _canvasBinders.Sort((left, right) =>
                {
                    if (left == null && right == null) return 0;
                    if (left == null) return 1;
                    if (right == null) return -1;

                    var typeCompare = string.CompareOrdinal(
                        left.GetType().FullName,
                        right.GetType().FullName);

                    if (typeCompare != 0)
                    {
                        return typeCompare;
                    }

                    return left.GetInstanceID().CompareTo(right.GetInstanceID());
                });
            }

            var cleanupCtx = CreateGameplayResetContext(request, GameplayResetStructs.Cleanup, serial);
            var restoreCtx = CreateGameplayResetContext(request, GameplayResetStructs.Restore, serial);
            var rebindCtx = CreateGameplayResetContext(request, GameplayResetStructs.Rebind, serial);

            foreach (var binder in _canvasBinders)
            {
                if (binder == null)
                {
                    continue;
                }

                await binder.Reset_CleanupAsync(cleanupCtx);
                await binder.Reset_RestoreAsync(restoreCtx);
                await binder.Reset_RebindAsync(rebindCtx);
            }
        }

        private readonly struct PlayerTarget
        {
            public PlayerTarget(string actorId, GameObject root, Transform transform)
            {
                ActorId = actorId;
                Root = root;
                Transform = transform;
            }

            public string ActorId { get; }

            public GameObject Root { get; }

            public Transform Transform { get; }
        }

        private readonly struct PlayerComponents
        {
            public PlayerComponents(
                PlayerMovementController movement,
                PlayerShootController shoot,
                PlayerInteractController interact,
                PlayerDetectionController detection)
            {
                Movement = movement;
                Shoot = shoot;
                Interact = interact;
                Detection = detection;
            }

            public PlayerMovementController Movement { get; }

            public PlayerShootController Shoot { get; }

            public PlayerInteractController Interact { get; }

            public PlayerDetectionController Detection { get; }
        }
    }
}
