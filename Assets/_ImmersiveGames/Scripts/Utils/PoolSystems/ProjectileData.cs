using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "New ProjectileData", menuName = "Pool/ProjectileData")]
    public class ProjectileData : PoolableObjectData
    {
        [SerializeField] private string objectName;
        [SerializeField] private GameObject logicPrefab; // Prefab com IPoolable
        [SerializeField] private GameObject modelPrefab; // Prefab com visual, colisão, etc.
        [SerializeField] private PoolableComponentType poolableComponentType;
        [SerializeField] private FactoryType factoryType;
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private float lifetime = 3f;

        public override string ObjectName => objectName;
        public override GameObject LogicPrefab => logicPrefab;
        public override GameObject ModelPrefab => modelPrefab;
        public override PoolableComponentType PoolableComponentType => poolableComponentType;
        public override FactoryType FactoryType => factoryType;
        public override int InitialPoolSize => initialPoolSize;
        public override float Lifetime => lifetime;
    }

    public abstract class PoolableObjectData : ScriptableObject
    {
        public abstract string ObjectName { get; }
        public abstract GameObject LogicPrefab { get; }
        public abstract GameObject ModelPrefab { get; }
        public abstract PoolableComponentType PoolableComponentType { get; }
        public abstract FactoryType FactoryType { get; }
        public abstract int InitialPoolSize { get; }
        public abstract float Lifetime { get; }
    }

    public enum PoolableComponentType { Bullet, Enemy, Effect }
    public enum FactoryType { Default, Custom }
}