using System;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class SkinController : MonoBehaviour
    {
        [SerializeField] private SkinCollectionData skinCollectionData;
        private SkinPoolableComponent _skinPoolable;
        public event Action<ISkinConfig> OnSkinChanged;
        public event Action<string> OnSkinChangeFailed;

        private void Awake()
        {
            if (skinCollectionData == null)
            {
                DebugUtility.LogError<SkinController>($"SkinCollectionData is not assigned in '{gameObject.name}'.", this);
                OnSkinChangeFailed?.Invoke("SkinCollectionData is not assigned.");
                return;
            }

            _skinPoolable = GetComponent<SkinPoolableComponent>();
            if (_skinPoolable == null)
            {
                _skinPoolable = gameObject.AddComponent<SkinPoolableComponent>();
                // Atribuir SkinCollectionData via campo SerializeField (já definido no Inspector)
                // Não é necessário chamar SetSkinCollectionData, pois o campo é configurado diretamente
            }

            // Validar se SkinPoolableComponent tem SkinCollectionData
            if (_skinPoolable.GetSkinCollectionData() == null)
            {
                DebugUtility.LogError<SkinController>($"SkinPoolableComponent in '{gameObject.name}' does not have a valid SkinCollectionData.", this);
                OnSkinChangeFailed?.Invoke("SkinPoolableComponent does not have a valid SkinCollectionData.");
            }
        }

        public void ChangeSkin(ISkinConfig newSkin)
        {
            if (_skinPoolable == null)
            {
                DebugUtility.LogError<SkinController>($"SkinPoolableComponent is not initialized in '{gameObject.name}'.", this);
                OnSkinChangeFailed?.Invoke("SkinPoolableComponent is not initialized.");
                return;
            }
            if (newSkin == null)
            {
                DebugUtility.LogError<SkinController>($"Invalid SkinConfig provided in '{gameObject.name}'.", this);
                OnSkinChangeFailed?.Invoke("Invalid SkinConfig provided.");
                return;
            }
            _skinPoolable.ApplySkin(newSkin);
            OnSkinChanged?.Invoke(newSkin);
        }

        public Transform GetContainer(ModelType modelType)
        {
            if (_skinPoolable == null)
            {
                DebugUtility.LogError<SkinController>($"SkinPoolableComponent is not initialized in '{gameObject.name}'.", this);
                return null;
            }
            return _skinPoolable.GetContainer(modelType);
        }

        public void ActivateSkin()
        {
            if (_skinPoolable != null)
            {
                _skinPoolable.Activate();
            }
        }

        public void DeactivateSkin()
        {
            if (_skinPoolable != null)
            {
                _skinPoolable.Deactivate();
            }
        }
    }
}