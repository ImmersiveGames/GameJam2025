using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core;
using ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Core;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Discovery
{
    public sealed class ActorGroupGameplayResetRegistryDiscoveryStrategy : IActorGroupGameplayResetDiscoveryStrategy
    {
        private readonly IActorRegistry _actorRegistry;
        private readonly IActorGroupGameplayResetTargetClassifier _classifier;

        public ActorGroupGameplayResetRegistryDiscoveryStrategy(IActorRegistry actorRegistry, IActorGroupGameplayResetTargetClassifier classifier)
        {
            _actorRegistry = actorRegistry;
            _classifier = classifier;
        }

        public string Name => "RegistryDiscovery";

        public int CollectTargets(ActorGroupGameplayResetRequest request, List<IActor> results, out bool fallbackUsed)
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



