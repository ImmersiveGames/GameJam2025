using System;
using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core
{
    internal sealed class ActorGroupRearmComponentResolver
    {
        private readonly string _sceneName;
        private readonly List<IActorGroupRearmable> _resettableBuffer = new(64);
        private readonly List<ResetEntry> _orderedResets = new(64);

        public ActorGroupRearmComponentResolver(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
        }

        public IReadOnlyList<ResetEntry> ResolveResettableComponents(ResetTarget target, ActorGroupRearmRequest request)
        {
            _orderedResets.Clear();
            _resettableBuffer.Clear();

            var root = target.Root;
            if (root == null)
            {
                return Array.Empty<ResetEntry>();
            }

            if (!string.IsNullOrWhiteSpace(_sceneName) && root.scene.name != _sceneName)
            {
                return Array.Empty<ResetEntry>();
            }

            MonoBehaviour[] monoBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            if (monoBehaviours == null || monoBehaviours.Length == 0)
            {
                return Array.Empty<ResetEntry>();
            }

            foreach (var mb in monoBehaviours)
            {
                if (mb == null)
                {
                    continue;
                }

                if (mb is IActorGroupRearmable resettable)
                {
                    if (resettable is IActorGroupRearmTargetFilter filter && !filter.ShouldParticipate(request.Target))
                    {
                        continue;
                    }

                    _resettableBuffer.Add(resettable);
                    continue;
                }

                if (mb is IActorGroupRearmableSync sync)
                {
                    if (sync is IActorGroupRearmTargetFilter syncFilter && !syncFilter.ShouldParticipate(request.Target))
                    {
                        continue;
                    }

                    _resettableBuffer.Add(new SyncAdapter(sync));
                }
            }

            if (_resettableBuffer.Count == 0)
            {
                return Array.Empty<ResetEntry>();
            }

            foreach (var component in _resettableBuffer)
            {
                int order = component is IActorGroupRearmOrder resetOrder ? resetOrder.ResetOrder : 0;
                _orderedResets.Add(new ResetEntry(component, order));
            }

            _orderedResets.Sort(CompareResetEntries);
            return _orderedResets;
        }

        private static int CompareResetEntries(ResetEntry left, ResetEntry right)
        {
            int orderCompare = left.Order.CompareTo(right.Order);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            string leftName = left.Component?.GetType().FullName;
            string rightName = right.Component?.GetType().FullName;
            int nameCompare = string.CompareOrdinal(leftName, rightName);
            if (nameCompare != 0)
            {
                return nameCompare;
            }

            int leftId = (left.Component as MonoBehaviour)?.GetInstanceID() ?? 0;
            int rightId = (right.Component as MonoBehaviour)?.GetInstanceID() ?? 0;
            return leftId.CompareTo(rightId);
        }
    }
}
