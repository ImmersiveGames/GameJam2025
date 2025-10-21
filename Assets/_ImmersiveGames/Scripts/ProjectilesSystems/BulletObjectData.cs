using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ProjectilesSystems
{
    [CreateAssetMenu(fileName = "BulletObjectData", menuName = "ImmersiveGames/BulletObjectData")]
    public class BulletObjectData : PoolableObjectData
    {
        [SerializeField] private float speed = 10f;

        public float Speed => speed;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!(speed < 0)) return;
            DebugUtility.LogWarning<BulletObjectData>($"Speed cannot be negative in {name}. Setting to 0.", this);
            speed = 0;
        }
#endif
    }
}