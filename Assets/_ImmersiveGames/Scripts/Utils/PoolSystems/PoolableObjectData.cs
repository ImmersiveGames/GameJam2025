using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public enum FactoryType
    {
        Default,
        Bullet,
        Enemy,
        Skin
    }

    public abstract class PoolableObjectData : ScriptableObject
    {
        [SerializeField] private string objectName;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private float lifetime = 5f;
        [SerializeField] private FactoryType modelFactory = FactoryType.Default;

        public string ObjectName => objectName;
        public GameObject Prefab => prefab;
        public float Lifetime => lifetime;
        public FactoryType ModelFactory => modelFactory;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (lifetime < 0)
            {
                Debug.LogWarning($"Lifetime não pode ser negativo em {name}. Definindo como 0.", this);
                lifetime = 0;
            }
        }
#endif
    }
}