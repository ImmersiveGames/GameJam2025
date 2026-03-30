using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Strategy
{
    public sealed class ActorGroupRearmRegistryDiscoveryStrategy : IActorGroupRearmDiscoveryStrategy
    {
        private readonly IActorRegistry _actorRegistry;
        private readonly IActorGroupRearmTargetClassifier _classifier;

        public ActorGroupRearmRegistryDiscoveryStrategy(IActorRegistry actorRegistry, IActorGroupRearmTargetClassifier classifier)
        {
            _actorRegistry = actorRegistry;
            _classifier = classifier;
        }

        public string Name => "RegistryDiscovery";

        public int CollectTargets(ActorGroupRearmRequest request, List<IActor> results, out bool fallbackUsed)
        {
            fallbackUsed = false;
            results.Clear();

            if (_actorRegistry == null || _classifier == null)
            {
                return 0;
            }

            _classifier.CollectTargets(request, _actorRegistry, results);
            return results.Count;
        }
    }
}

