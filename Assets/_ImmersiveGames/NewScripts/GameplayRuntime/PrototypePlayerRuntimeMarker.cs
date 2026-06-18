using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime
{
    public sealed class PrototypePlayerRuntimeMarker : MonoBehaviour
    {
        [SerializeField] private string playerId;
        [SerializeField] private string routeIdentity;
        [SerializeField] private string routeOperationId;
        [SerializeField] private string transitionId;
        [SerializeField] private int routeSequence;

        public string PlayerId => playerId;
        public string RouteIdentity => routeIdentity;
        public string RouteOperationId => routeOperationId;
        public string TransitionId => transitionId;
        public int RouteSequence => routeSequence;

        public void Bind(string bindPlayerId, string bindRouteIdentity, string bindRouteOperationId, string bindTransitionId, int bindRouteSequence)
        {
            playerId = bindPlayerId.TrimToEmpty();
            routeIdentity = bindRouteIdentity.TrimToEmpty();
            routeOperationId = bindRouteOperationId.TrimToEmpty();
            transitionId = bindTransitionId.TrimToEmpty();
            routeSequence = bindRouteSequence < 0 ? 0 : bindRouteSequence;
        }
    }
}
