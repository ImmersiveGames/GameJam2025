using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils
{
    public class UniqueIdFactory : MonoBehaviour
    {
        private static UniqueIdFactory _instance;
        public static UniqueIdFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UniqueIdFactory>();
                    if (_instance == null)
                    {
                        _instance = new GameObject("UniqueIdFactory").AddComponent<UniqueIdFactory>();
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }
                return _instance;
            }
        }

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
                if (!_instanceCounts.ContainsKey(baseActorName))
                    _instanceCounts[baseActorName] = 0;

                int instanceId = _instanceCounts[baseActorName]++;
                actorId = $"NPC_{baseActorName}_{instanceId}";
                DebugUtility.LogVerbose<UniqueIdFactory>($"GenerateId: ActorName={actor.Name}, InstanceId={instanceId}, ActorId={actorId}, BaseId={baseId}, UniqueId={actorId}_{baseId}");
            }

            return $"{actorId}_{baseId}";
        }

        public int GetInstanceCount(string actorName)
        {
            return _instanceCounts.TryGetValue(actorName, out int count) ? count : 0;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instanceCounts.Clear();
                _instance = null;
            }
        }
    }
}