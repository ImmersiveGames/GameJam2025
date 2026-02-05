using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm
{
    public sealed class RegistryActorDiscoveryStrategy : IActorDiscoveryStrategy
    {
        private readonly IActorRegistry _actorRegistry;
        private readonly IGameplayResetTargetClassifier _classifier;

        public RegistryActorDiscoveryStrategy(IActorRegistry actorRegistry, IGameplayResetTargetClassifier classifier)
        {
            _actorRegistry = actorRegistry;
            _classifier = classifier;
        }

        public string Name => "RegistryDiscovery";

        public int CollectTargets(GameplayResetRequest request, List<IActor> results, out bool fallbackUsed)
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
