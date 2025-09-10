using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "BulletObjectData", menuName = "ImmersiveGames/BulletObjectData")]
    public class BulletObjectData : PoolableObjectData
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private Vector3 initialDirection = Vector3.forward;

        public float Speed => speed;
        public Vector3 InitialDirection => initialDirection;

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!(speed < 0)) return;
            Debug.LogWarning($"Speed cannot be negative in {name}. Setting to 0.", this);
            speed = 0;
        }
    #endif
    }
}