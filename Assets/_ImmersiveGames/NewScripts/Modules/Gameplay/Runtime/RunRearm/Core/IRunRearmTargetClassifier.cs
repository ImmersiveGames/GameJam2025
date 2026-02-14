using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.RunRearm.Core
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
