using System;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils
{
    public class UniqueIdFactory : MonoBehaviour
    {
        private static UniqueIdFactory _instance;
        private int _genericCounter = 0;

        public static UniqueIdFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UniqueIdFactory");
                    _instance = go.AddComponent<UniqueIdFactory>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public string GenerateId(GameObject source, string baseId, bool useActor = true)
        {
            if (useActor)
            {
                var actor = source.GetComponentInParent<_ImmersiveGames.Scripts.ActorSystems.IActor>();
                if (actor != null && !string.IsNullOrEmpty(actor.Name))
                {
                    return $"{actor.Name}_{baseId}";
                }
            }
            // Para objetos sem IActor, usa GUID ou contador
            return $"{baseId}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            // Alternativa: $"Generic_{_genericCounter++}_{baseId}";
        }

        // Gera ID sem referência a GameObject (ex.: para managers ou UI)
        public string GenerateGenericId(string baseId)
        {
            return $"{baseId}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}