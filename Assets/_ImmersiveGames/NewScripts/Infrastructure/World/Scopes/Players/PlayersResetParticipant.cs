using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using LegacyActor = _ImmersiveGames.Scripts.ActorSystems.IActor;
using NewActor = _ImmersiveGames.NewScripts.Infrastructure.Actors.IActor;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    public sealed class PlayersResetParticipant : IResetScopeParticipant
    {
        private readonly List<PlayerTarget> _playerTargets = new(8);
        private readonly List<NewActor> _actorBuffer = new(16);
        private readonly List<LegacyActor> _legacyPlayerBuffer = new(16);
        private readonly List<IResetInterfaces> _resetBuffer = new(16);
        private readonly List<ResetParticipantEntry> _orderedResets = new(32);

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
                var components = ResolveResettableComponents(target);
                await RunPhaseAsync(target, components, GameplayResetStructs.Cleanup, request, serial);
                await RunPhaseAsync(target, components, GameplayResetStructs.Restore, request, serial);
                await RunPhaseAsync(target, components, GameplayResetStructs.Rebind, request, serial);
            }

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
            IReadOnlyList<ResetParticipantEntry> components,
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

            await InvokePhaseAsync(components, phase, ctx);

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

        private IReadOnlyList<ResetParticipantEntry> ResolveResettableComponents(PlayerTarget target)
        {
            var actorId = string.IsNullOrWhiteSpace(target.ActorId) ? "<unknown>" : target.ActorId;
            _orderedResets.Clear();
            _resetBuffer.Clear();

            var root = target.Root;

            if (root == null)
            {
                return Array.Empty<ResetParticipantEntry>();
            }

            var monoBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            if (monoBehaviours == null || monoBehaviours.Length == 0)
            {
                return Array.Empty<ResetParticipantEntry>();
            }

            foreach (var behaviour in monoBehaviours)
            {
                if (behaviour is not IResetInterfaces resettable)
                {
                    continue;
                }

                if (resettable is IResetScopeFilter filter &&
                    !filter.ShouldParticipate(GameplayResetScope.PlayersOnly))
                {
                    continue;
                }

                _resetBuffer.Add(resettable);
            }

            if (_resetBuffer.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                    $"[PlayersResetParticipant] Resetables collected (actorId={actorId}, count=0)");
                return Array.Empty<ResetParticipantEntry>();
            }

            foreach (var resettable in _resetBuffer)
            {
                var order = resettable is IResetOrder resetOrder ? resetOrder.ResetOrder : 0;
                _orderedResets.Add(new ResetParticipantEntry(resettable, order));
            }

            _orderedResets.Sort((left, right) =>
            {
                var orderCompare = left.Order.CompareTo(right.Order);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }

                var leftName = left.Component?.GetType().FullName;
                var rightName = right.Component?.GetType().FullName;
                var nameCompare = string.CompareOrdinal(leftName, rightName);
                if (nameCompare != 0)
                {
                    return nameCompare;
                }

                var leftId = (left.Component as MonoBehaviour)?.GetInstanceID() ?? 0;
                var rightId = (right.Component as MonoBehaviour)?.GetInstanceID() ?? 0;
                return leftId.CompareTo(rightId);
            });

            var typesLabel = string.Join(", ",
                _orderedResets.Select(entry => entry.Component?.GetType().Name ?? "<null>"));

            DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Resetables collected (actorId={actorId}, count={_orderedResets.Count}) => {typesLabel}");

            return _orderedResets;
        }

        private static Task InvokePhaseAsync(
            IReadOnlyList<ResetParticipantEntry> components,
            GameplayResetStructs phase,
            GameplayResetContext ctx)
        {
            if (components == null || components.Count == 0)
            {
                return Task.CompletedTask;
            }

            switch (phase)
            {
                case GameplayResetStructs.Cleanup:
                    return RunPhaseInternal(components, (component, context) => component.Reset_CleanupAsync(context), ctx);
                case GameplayResetStructs.Restore:
                    return RunPhaseInternal(components, (component, context) => component.Reset_RestoreAsync(context), ctx);
                case GameplayResetStructs.Rebind:
                    return RunPhaseInternal(components, (component, context) => component.Reset_RebindAsync(context), ctx);
                default:
                    return Task.CompletedTask;
            }
        }

        private static async Task RunPhaseInternal(
            IReadOnlyList<ResetParticipantEntry> components,
            Func<IResetInterfaces, GameplayResetContext, Task> action,
            GameplayResetContext ctx)
        {
            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i].Component;
                if (component == null)
                {
                    continue;
                }

                await action(component, ctx);
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

        private readonly struct ResetParticipantEntry
        {
            public ResetParticipantEntry(IResetInterfaces component, int order)
            {
                Component = component;
                Order = order;
            }

            public IResetInterfaces Component { get; }

            public int Order { get; }
        }
    }
}
