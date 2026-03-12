using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core
{
    public sealed class RegistryActorGroupRearmDiscoveryStrategy : IActorGroupRearmDiscoveryStrategy
    {
        private readonly IActorRegistry _actorRegistry;
        private readonly IActorGroupRearmTargetClassifier _classifier;

        public RegistryActorGroupRearmDiscoveryStrategy(IActorRegistry actorRegistry, IActorGroupRearmTargetClassifier classifier)
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

