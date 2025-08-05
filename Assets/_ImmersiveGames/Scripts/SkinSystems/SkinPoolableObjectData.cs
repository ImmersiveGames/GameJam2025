using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    [CreateAssetMenu(fileName = "SkinPoolableObjectData", menuName = "ImmersiveGames/Skin/SkinPoolableObjectData", order = 5)]
    public class SkinPoolableObjectData : PoolableObjectData
    {
        [SerializeField] private ModelType modelType;
        [SerializeField] private FactoryType modelFactory = FactoryType.Skin; // Usa fábrica específica para skins

        public ModelType ModelType => modelType;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (Prefab == null)
            {
                Debug.LogWarning($"Prefab não configurado em {name}.", this);
            }
        }
#endif
    }
}