using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.RunRearm.Core
{
    public sealed class RegistryActorDiscoveryStrategy : IActorDiscoveryStrategy
    {
        private readonly IActorRegistry _actorRegistry;
        private readonly IRunRearmTargetClassifier _classifier;

        public RegistryActorDiscoveryStrategy(IActorRegistry actorRegistry, IRunRearmTargetClassifier classifier)
        {
            _actorRegistry = actorRegistry;
            _classifier = classifier;
        }

        public string Name => "RegistryDiscovery";

        public int CollectTargets(RunRearmRequest request, List<IActor> results, out bool fallbackUsed)
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
