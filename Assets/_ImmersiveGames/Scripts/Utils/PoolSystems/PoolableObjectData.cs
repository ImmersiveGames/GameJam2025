using System.Collections.Generic;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "PoolableObjectData", menuName = "PoolableObjectData")]
    public class PoolableObjectData : ScriptableObject
    {
        [SerializeField] private string objectName;
        [SerializeField] private GameObject prefab;
        [SerializeField] private List<GameObject> modelPrefab;
        [SerializeField] private bool noRandomModel = false;
        [SerializeField, Min(0)] private float lifetime = 5f; // Validação no Inspector
        [SerializeField] private int initialPoolSize = 5;
        [SerializeField] private FactoryType factoryType = FactoryType.Default;
        [SerializeField] private bool canExpand = false;

        public string ObjectName => objectName;
        public GameObject Prefab => prefab;
        public GameObject ModelPrefab => noRandomModel? modelPrefab[0] : modelPrefab.Random();
        public int InitialPoolSize => initialPoolSize;
        public FactoryType FactoryType => factoryType;
        public bool CanExpand => canExpand;
        public float Lifetime => lifetime;

#if UNITY_EDITOR
        private void OnValidate()
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