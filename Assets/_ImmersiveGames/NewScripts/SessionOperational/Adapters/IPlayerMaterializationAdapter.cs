using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public readonly struct PlayerMaterializationRequest
    {
        public PlayerMaterializationRequest(string playerId, bool required, GameObject prefab)
        {
            PlayerId = string.IsNullOrWhiteSpace(playerId) ? string.Empty : playerId.Trim();
            Required = required;
            Prefab = prefab;
        }

        public string PlayerId { get; }
        public bool Required { get; }
        public GameObject Prefab { get; }
        public bool HasPrefab => Prefab != null;
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId);
    }

    public readonly struct PlayerMaterializationCommand
    {
        public PlayerMaterializationCommand(PlayerPreparationIdentity identity, string routeOperationId, string source, string reason, IReadOnlyList<PlayerMaterializationRequest> requests)
        {
            Identity = identity;
            RouteOperationId = string.IsNullOrWhiteSpace(routeOperationId) ? string.Empty : routeOperationId.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Requests = requests ?? System.Array.Empty<PlayerMaterializationRequest>();
        }

        public PlayerPreparationIdentity Identity { get; }
        public string RouteOperationId { get; }
        public string Source { get; }
        public string Reason { get; }
        public IReadOnlyList<PlayerMaterializationRequest> Requests { get; }
        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(RouteOperationId) && !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(Reason) && Requests != null;
    }

    public interface IPlayerMaterializationAdapter
    {
        IReadOnlyList<PlayerMaterializationRecord> MaterializePrototypePlayers(PlayerMaterializationCommand command, SessionOperationalRouteCommand routeCommand);
    }
}
