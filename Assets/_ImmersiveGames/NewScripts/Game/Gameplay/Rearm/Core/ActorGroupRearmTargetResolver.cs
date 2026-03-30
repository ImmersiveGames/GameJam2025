using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Strategy;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core
{
    internal sealed class ActorGroupRearmTargetResolver
    {
        private readonly string _sceneName;
        private readonly IWorldResetPolicy _worldResetPolicy;
        private readonly IActorGroupRearmDiscoveryStrategy _registryDiscovery;
        private readonly IActorGroupRearmDiscoveryStrategy _sceneScanDiscovery;
        private readonly List<IActor> _actorBuffer = new(32);
        private readonly List<ResetTarget> _targets = new(32);

        public ActorGroupRearmTargetResolver(
            string sceneName,
            IWorldResetPolicy worldResetPolicy,
            IActorGroupRearmDiscoveryStrategy registryDiscovery,
            IActorGroupRearmDiscoveryStrategy sceneScanDiscovery)
        {
            _sceneName = sceneName ?? string.Empty;
            _worldResetPolicy = worldResetPolicy;
            _registryDiscovery = registryDiscovery;
            _sceneScanDiscovery = sceneScanDiscovery;
        }

        public IReadOnlyList<ResetTarget> ResolveTargets(
            ActorGroupRearmRequest request,
            out bool usedSceneScan,
            out bool scanDisabled)
        {
            _targets.Clear();
            usedSceneScan = false;
            scanDisabled = false;

            if (_registryDiscovery != null)
            {
                _actorBuffer.Clear();
                _registryDiscovery.CollectTargets(request, _actorBuffer, out _);

                if (_actorBuffer.Count > 0)
                {
                    foreach (var actor in _actorBuffer)
                    {
                        TryAddTargetFromActor(actor);
                    }

                    SortTargets();
                    return _targets;
                }
            }

            if (_worldResetPolicy != null && _worldResetPolicy.AllowSceneScan && _sceneScanDiscovery != null)
            {
                _actorBuffer.Clear();
                _sceneScanDiscovery.CollectTargets(request, _actorBuffer, out _);
                usedSceneScan = true;

                if (_actorBuffer.Count > 0)
                {
                    foreach (var actor in _actorBuffer)
                    {
                        TryAddTargetFromActor(actor);
                    }
                }
            }
            else
            {
                scanDisabled = true;
            }

            SortTargets();
            return _targets;
        }

        private void TryAddTargetFromActor(IActor actor)
        {
            if (actor == null)
            {
                return;
            }

            var transform = actor.Transform;
            if (transform == null)
            {
                return;
            }

            var root = transform.gameObject;
            if (root == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_sceneName) && root.scene.name != _sceneName)
            {
                return;
            }

            string actorId = actor.ActorId ?? string.Empty;
            _targets.Add(new ResetTarget(actorId, root, transform));
        }

        private void SortTargets()
        {
            if (_targets.Count <= 1)
            {
                return;
            }

            _targets.Sort((left, right) => string.CompareOrdinal(left.ActorId, right.ActorId));
        }
    }
}
