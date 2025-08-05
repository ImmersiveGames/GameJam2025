using System;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinController : MonoBehaviour
    {
        [SerializeField] private SkinCollectionData skinCollectionData;
        private SkinModelBuild _skinModelBuild;
        public event Action<ISkinConfig> OnSkinChanged;
        public event Action<string> OnSkinChangeFailed;

        private void Start()
        {
            if (skinCollectionData == null)
            {
                Debug.LogError($"SkinCollectionData is not assigned in SkinController on '{gameObject.name}'.", this);
                OnSkinChangeFailed?.Invoke("SkinCollectionData is not assigned.");
                return;
            }

            if (_skinModelBuild == null)
            {
                _skinModelBuild = new SkinModelBuild(skinCollectionData, transform);
            }
            else
            {
                Debug.LogWarning($"SkinModelBuild already initialized in SkinController on '{gameObject.name}'. Skipping re-initialization.", this);
            }
        }

        public void ChangeSkin(ISkinConfig newSkin)
        {
            if (_skinModelBuild == null)
            {
                Debug.LogError($"SkinModelBuild is not initialized in SkinController on '{gameObject.name}'.");
                OnSkinChangeFailed?.Invoke("SkinModelBuild is not initialized.");
                return;
            }
            if (newSkin == null)
            {
                Debug.LogError($"Invalid SkinConfig provided in SkinController on '{gameObject.name}'.");
                OnSkinChangeFailed?.Invoke("Invalid SkinConfig provided.");
                return;
            }
            _skinModelBuild.ApplySkin(newSkin);
            OnSkinChanged?.Invoke(newSkin);
        }

        public Transform GetContainer(ModelType modelType)
        {
            if (_skinModelBuild == null)
            {
                Debug.LogError($"SkinModelBuild is not initialized in SkinController on '{gameObject.name}'.");
                return null;
            }
            return _skinModelBuild.GetContainer(modelType);
        }
    }
}