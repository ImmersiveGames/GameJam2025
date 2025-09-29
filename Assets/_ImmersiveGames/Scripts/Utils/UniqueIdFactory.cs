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
            string actorId = "";
            string baseActorName = source.name;

            if (actor != null)
            {
                baseActorName = actor.ActorName;
                var ownActor = source.GetComponent<IActor>();

                if (ownActor != null)
                {
                    // Actor principal (root): sempre gerar novo ID, ignorando ActorId atual (evita circularidade)
                    DebugUtility.LogVerbose<UniqueIdFactory>($"Generating new ID for main actor {source.name}.");
                    var playerInput = source.GetComponent<PlayerInput>();
                    if (playerInput != null)
                    {
                        actorId = $"Player_{playerInput.playerIndex}";
                    }
                    else
                    {
                        _instanceCounts.TryAdd(baseActorName, 0);
                        int instanceId = _instanceCounts[baseActorName]++;
                        actorId = $"NPC_{baseActorName}_{instanceId}";
                    }
                }
                else if (!string.IsNullOrEmpty(actor.ActorId))
                {
                    // True child (sem own IActor): reuse parent ActorId
                    actorId = actor.ActorId;
                    DebugUtility.LogVerbose<UniqueIdFactory>($"Reusing parent ActorId '{actorId}' for child {source.name}.");
                }
                else
                {
                    // Fallback se parent ActorId vazio (deve ser raro)
                    DebugUtility.LogWarning<UniqueIdFactory>($"Fallback to empty ActorId for {source.name} (parent ActorId empty).");
                }
            }
            else
            {
                // Non-actor: gerar como obj
                DebugUtility.LogWarning<UniqueIdFactory>($"No IActor found for {source.name}. Using object name as base.");
                _instanceCounts.TryAdd(baseActorName, 0);
                int instanceId = _instanceCounts[baseActorName]++;
                actorId = $"Obj_{baseActorName}_{instanceId}";
            }

            string uniqueId = string.IsNullOrEmpty(baseId) ? actorId : $"{actorId}_{baseId}";
            DebugUtility.LogVerbose<UniqueIdFactory>($"Generated ID: {uniqueId} (baseActorName={baseActorName}, baseId={baseId})");
            return uniqueId;
        }

        public int GetInstanceCount(string actorName)
        {
            return _instanceCounts.GetValueOrDefault(actorName, 0);
        }
    }
}