using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges;
using _ImmersiveGames.Scripts.Utils.CameraSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameplayResetScope = _ImmersiveGames.Scripts.GameplaySystems.Reset.ResetScope;

namespace _ImmersiveGames.NewScripts.Gameplay.Bridges.Reset
{
    /// <summary>
    /// Builder dedicado para compor a carga útil do ResetScope.Players,
    /// isolando o uso de componentes legados.
    /// </summary>
    internal sealed class PlayersResetPayloadBuilder
    {
        private static readonly Type[] SceneLevelParticipantTypes =
        {
            typeof(CanvasCameraBinder),
            typeof(RuntimeAttributeController),
            typeof(RuntimeAttributeAutoFlowBridge)
        };

        private readonly List<ResetTargetPayload> _payloads = new(8);
        private readonly HashSet<IResetInterfaces> _sceneLevelUniques = new(ReferenceEqualityComparer<IResetInterfaces>.Instance);

        public IReadOnlyList<ResetTargetPayload> Build(
            IReadOnlyList<PlayerResetTarget> playerTargets,
            string sceneName)
        {
            _payloads.Clear();
            _sceneLevelUniques.Clear();

            if (playerTargets is { Count: > 0 })
            {
                foreach (var target in playerTargets)
                {
                    var collected = CollectFromRoot(target.RootTransform, target.ActorId, isSceneLevel: false, playerTargets);
                    _payloads.Add(new ResetTargetPayload(target.Label, collected, isSceneLevel: false));
                }
            }

            var scene = string.IsNullOrWhiteSpace(sceneName)
                ? SceneManager.GetActiveScene()
                : SceneManager.GetSceneByName(sceneName);

            if (scene.IsValid() && scene.isLoaded)
            {
                var sceneLevel = CollectSceneLevel(scene, playerTargets);
                if (sceneLevel.Count > 0)
                {
                    var label = $"ScenePlayersBaseline({scene.name})";
                    _payloads.Add(new ResetTargetPayload(label, sceneLevel, isSceneLevel: true));
                }
            }

            return _payloads;
        }

        private IReadOnlyList<ResetParticipantEntry> CollectSceneLevel(
            Scene scene,
            IReadOnlyList<PlayerResetTarget> playerTargets)
        {
            var aggregated = new List<ResetParticipantEntry>();

            var roots = scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                return Array.Empty<ResetParticipantEntry>();
            }

            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var collected = CollectFromRoot(root.transform, sourceActorId: null, isSceneLevel: true, playerTargets);
                if (collected.Count > 0)
                {
                    aggregated.AddRange(collected);
                }
            }

            SortResetEntries(aggregated);
            return aggregated.ToArray();
        }

        private IReadOnlyList<ResetParticipantEntry> CollectFromRoot(
            Transform root,
            string sourceActorId,
            bool isSceneLevel,
            IReadOnlyList<PlayerResetTarget> playerTargets)
        {
            var entries = new List<ResetParticipantEntry>();
            var localUniques = isSceneLevel ? _sceneLevelUniques : new HashSet<IResetInterfaces>(ReferenceEqualityComparer<IResetInterfaces>.Instance);

            if (root == null)
            {
                return Array.Empty<ResetParticipantEntry>();
            }

            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return Array.Empty<ResetParticipantEntry>();
            }

            for (var index = 0; index < behaviours.Length; index++)
            {
                var behaviour = behaviours[index];
                if (behaviour is not IResetInterfaces resettable)
                {
                    continue;
                }

                if (!IsEligible(resettable, behaviour, isSceneLevel, playerTargets))
                {
                    continue;
                }

                if (!localUniques.Add(resettable))
                {
                    continue;
                }

                var order = resettable is IResetOrder resetOrder ? resetOrder.ResetOrder : 0;
                var sourceLabel = BuildSourceLabel(behaviour.transform, sourceActorId);

                entries.Add(new ResetParticipantEntry(resettable, order, sourceLabel));
            }

            SortResetEntries(entries);
            return entries.ToArray();
        }

        private static bool IsEligible(
            IResetInterfaces resettable,
            MonoBehaviour behaviour,
            bool isSceneLevel,
            IReadOnlyList<PlayerResetTarget> playerTargets)
        {
            if (resettable is IResetScopeFilter filter &&
                !filter.ShouldParticipate(GameplayResetScope.PlayersOnly))
            {
                return false;
            }

            if (!isSceneLevel)
            {
                return true;
            }

            if (!IsSceneLevelType(behaviour))
            {
                return false;
            }

            return !IsUnderAnyPlayerRoot(behaviour.transform, playerTargets);
        }

        private static bool IsSceneLevelType(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return false;
            }

            var type = behaviour.GetType();
            for (var i = 0; i < SceneLevelParticipantTypes.Length; i++)
            {
                if (SceneLevelParticipantTypes[i].IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUnderAnyPlayerRoot(Transform transform, IReadOnlyList<PlayerResetTarget> playerTargets)
        {
            if (transform == null || playerTargets == null || playerTargets.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < playerTargets.Count; i++)
            {
                var root = playerTargets[i].RootTransform;
                if (root == null)
                {
                    continue;
                }

                if (transform == root || transform.IsChildOf(root))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SortResetEntries(List<ResetParticipantEntry> entries)
        {
            if (entries == null || entries.Count <= 1)
            {
                return;
            }

            entries.Sort((left, right) =>
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
        }

        private static string BuildSourceLabel(Transform transform, string actorId)
        {
            var path = BuildTransformPath(transform);
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return path;
            }

            return $"{actorId}:{path}";
        }

        internal static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            var stack = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }
    }

    internal readonly struct PlayerResetTarget
    {
        public PlayerResetTarget(string actorId, Transform rootTransform)
        {
            ActorId = actorId ?? string.Empty;
            RootTransform = rootTransform;
        }

        public string ActorId { get; }

        public Transform RootTransform { get; }

        public string Label => string.IsNullOrWhiteSpace(ActorId)
            ? $"Player(<unknown>):{PlayersResetPayloadBuilder.BuildTransformPath(RootTransform)}"
            : $"Player({ActorId}):{PlayersResetPayloadBuilder.BuildTransformPath(RootTransform)}";
    }

    internal readonly struct ResetParticipantEntry
    {
        public ResetParticipantEntry(IResetInterfaces component, int order, string sourceLabel)
        {
            Component = component;
            Order = order;
            SourceLabel = sourceLabel;
        }

        public IResetInterfaces Component { get; }

        public int Order { get; }

        public string SourceLabel { get; }
    }

    internal sealed class ResetTargetPayload
    {
        public ResetTargetPayload(string label, IReadOnlyList<ResetParticipantEntry> components, bool isSceneLevel)
        {
            Label = string.IsNullOrWhiteSpace(label) ? "<unknown>" : label;
            Components = components ?? Array.Empty<ResetParticipantEntry>();
            IsSceneLevel = isSceneLevel;
        }

        public string Label { get; }

        public IReadOnlyList<ResetParticipantEntry> Components { get; }

        public bool IsSceneLevel { get; }
    }

    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new();

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return obj != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj) : 0;
        }
    }
}
