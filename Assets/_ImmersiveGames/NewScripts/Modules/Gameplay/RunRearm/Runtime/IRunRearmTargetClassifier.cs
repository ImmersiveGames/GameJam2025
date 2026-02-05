using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm.Runtime
{
    /// <summary>
    /// Resolve quais atores devem participar de um RunRearmRequest.
    /// Mantém a lógica de "grupos/alvos" centralizada e testável.
    /// </summary>
    public interface IRunRearmTargetClassifier
    {
        void CollectTargets(RunRearmRequest request, IActorRegistry actorRegistry, List<IActor> results);
    }
}
