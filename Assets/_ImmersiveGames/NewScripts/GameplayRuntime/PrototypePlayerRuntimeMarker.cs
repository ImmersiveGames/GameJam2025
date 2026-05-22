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
            playerId = string.IsNullOrWhiteSpace(bindPlayerId) ? string.Empty : bindPlayerId.Trim();
            routeIdentity = string.IsNullOrWhiteSpace(bindRouteIdentity) ? string.Empty : bindRouteIdentity.Trim();
            routeOperationId = string.IsNullOrWhiteSpace(bindRouteOperationId) ? string.Empty : bindRouteOperationId.Trim();
            transitionId = string.IsNullOrWhiteSpace(bindTransitionId) ? string.Empty : bindTransitionId.Trim();
            routeSequence = bindRouteSequence < 0 ? 0 : bindRouteSequence;
        }
    }
}
