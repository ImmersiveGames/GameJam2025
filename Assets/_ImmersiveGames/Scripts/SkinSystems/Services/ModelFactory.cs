using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class ModelFactory
    {
        public GameObject Instantiate(GameObject prefab, Transform parent, string skinName, ModelType type, IActor spawner)
        {
            if (prefab == null || parent == null) return null;

            var instance = Object.Instantiate(prefab, parent);
            instance.name = $"{skinName}_{type}";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }
    }
}