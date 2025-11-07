using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public abstract class PoolableObjectData : ScriptableObject
    {
        [SerializeField] private string objectName;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private float lifetime = 5f;

        public string ObjectName => objectName;
        public GameObject Prefab => prefab;
        public float Lifetime => lifetime;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (lifetime < 0)
            {
                DebugUtility.LogWarning<PoolableObjectData>($"Lifetime não pode ser negativo em {name}. Definindo como 0.", this);
                lifetime = 0;
            }
        }
#endif
    }
}