using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils
{
    public interface IUniqueIdFactory
    {
        string GenerateId(GameObject source, string baseId);
        int GetInstanceCount(string actorName);
    }
    public class UniqueIdFactory : IUniqueIdFactory
    {

        private readonly Dictionary<string, int> _instanceCounts = new Dictionary<string, int>();

        public string GenerateId(GameObject source, string baseId)
        {
            var actor = source.GetComponentInParent<IActor>();
            if (actor == null)
            {
                DebugUtility.LogError<UniqueIdFactory>("IActor não encontrado para gerar ID!", source);
                return baseId;
            }

            string actorId;
            var playerInput = source.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                actorId = $"Player_{playerInput.playerIndex}";
                DebugUtility.LogVerbose<UniqueIdFactory>($"GenerateId: ActorName={actor.Name}, PlayerIndex={playerInput.playerIndex}, ActorId={actorId}, BaseId={baseId}, UniqueId={actorId}_{baseId}");
            }
            else
            {
                string baseActorName = actor.Name;
                _instanceCounts.TryAdd(baseActorName, 0);

                int instanceId = _instanceCounts[baseActorName]++;
                actorId = $"NPC_{baseActorName}_{instanceId}";
                DebugUtility.LogVerbose<UniqueIdFactory>($"GenerateId: ActorName={actor.Name}, InstanceId={instanceId}, ActorId={actorId}, BaseId={baseId}, UniqueId={actorId}_{baseId}");
            }

            return $"{actorId}_{baseId}";
        }

        public int GetInstanceCount(string actorName)
        {
            return _instanceCounts.GetValueOrDefault(actorName, 0);
        }
    }
}