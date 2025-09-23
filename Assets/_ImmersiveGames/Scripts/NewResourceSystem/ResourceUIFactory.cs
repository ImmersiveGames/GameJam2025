using _ImmersiveGames.Scripts.NewResourceSystem.Events;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.NewResourceSystem
{
    /// <summary>
    /// responsavel por criar os objetos de recursos.
    /// Implementa o padrão Factory para instanciar prefabs de UI de recursos.
    /// </summary>
    [DebugLevel(DebugLevel.Error)]
    public class ResourceUIFactory : MonoBehaviour, IUIFactory<ResourceBindEvent, IResourceUI>
    {
        [SerializeField] private GameObject resourceUIPrefab;
        
        public IResourceUI CreateUI(ResourceBindEvent evt, Transform parent)
        {
            if (resourceUIPrefab == null)
            {
                DebugUtility.LogError<ResourceUIFactory>("❌ ResourceUI Prefab not assigned!");
                return null;
            }

            DebugUtility.LogVerbose<ResourceUIFactory>($"🏭 Instantiating prefab: {resourceUIPrefab.name}");
            var instance = Instantiate(resourceUIPrefab, parent);
            DebugUtility.LogVerbose<ResourceUIFactory>($"🏭 Instance created: {instance.name}");

            var resourceUI = instance.GetComponent<IResourceUI>();

            if (resourceUI == null)
            {
                DebugUtility.LogError<ResourceUIFactory>("❌ Prefab doesn't have IResourceUI component!");
                return null;
            }

            DebugUtility.LogVerbose<ResourceUIFactory>($"🏭 IResourceUI component found!");
            return resourceUI;
        }

        public void ReturnToPool(IResourceUI ui)
        {
            if (ui is MonoBehaviour behaviour)
                Destroy(behaviour.gameObject);
        }
    }
}