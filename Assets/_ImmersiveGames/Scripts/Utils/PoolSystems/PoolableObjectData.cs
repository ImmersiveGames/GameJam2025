using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "PoolableObjectData", menuName = "PoolableObjectData")]
    public class PoolableObjectData : ScriptableObject
    {
        [SerializeField] private  string objectName;
        [SerializeField] private  GameObject prefab;
        [SerializeField] private  GameObject modelPrefab;
        [SerializeField] private  int initialPoolSize = 5;
        [SerializeField] private  FactoryType factoryType = FactoryType.Default;
        [SerializeField] private bool canExpand = false; // Novo: controla expansão

        public string ObjectName => objectName;
        public GameObject Prefab => prefab;
        public GameObject ModelPrefab => modelPrefab;
        public int InitialPoolSize => initialPoolSize;
        public FactoryType FactoryType => factoryType;
        public bool CanExpand => canExpand;
        
    }
}