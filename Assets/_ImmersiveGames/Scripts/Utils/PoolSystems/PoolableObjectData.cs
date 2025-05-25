using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "PoolableObjectData", menuName = "PoolableObjectData")]
    public class PoolableObjectData : ScriptableObject
    {
        public string ObjectName;
        public GameObject LogicPrefab;
        public GameObject ModelPrefab;
        public int InitialPoolSize = 5;
        public FactoryType FactoryType = FactoryType.Default;
    }
}