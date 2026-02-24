using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Services
{
    public class SkinModelFactory
    {
        public GameObject Instantiate(GameObject prefab, Transform parent, IActor spawner, ISkinConfig config)
        {
            if (prefab == null || parent == null) return null;

            var instance = Object.Instantiate(prefab, parent);
            instance.name = $"{config.ConfigName}_{config.ModelType}";
            ApplyTransformConfig(instance.transform, config);
            instance.SetActive(config.GetActiveState());
            return instance;
        }
        
        private void ApplyTransformConfig(Transform target, ISkinConfig config)
        {
            target.localPosition = config.GetPosition();
            target.localRotation = Quaternion.Euler(config.GetRotation());
            target.localScale = config.GetScale();
        }
    }
}