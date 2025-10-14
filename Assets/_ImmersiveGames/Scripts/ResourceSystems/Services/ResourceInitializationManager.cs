using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    public class ResourceInitializationManager : PersistentSingleton<ResourceInitializationManager>
    {
        public readonly Dictionary<string, IInjectableComponent> _pendingComponents = new();

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            DebugUtility.LogVerbose<ResourceInitializationManager>("✅ Initialization Manager Ready");
        }
    
        public void RegisterForInjection(IInjectableComponent component)
        {
            string objectId = component.GetObjectId();
            _pendingComponents[objectId] = component;
        
            StartCoroutine(InjectionRetryRoutine(component));
        }
    
        private IEnumerator InjectionRetryRoutine(IInjectableComponent component)
        {
            string objectId = component.GetObjectId();
            int maxAttempts = 10;
        
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try 
                {
                    DependencyManager.Instance.InjectDependencies(component, objectId);
                    component.OnDependenciesInjected();
                    _pendingComponents.Remove(objectId);
                    yield break;
                }
                catch (Exception ex)
                {
                    if (attempt == maxAttempts - 1)
                    {
                        DebugUtility.LogError<ResourceInitializationManager>(
                            $"Failed to inject dependencies for {component.GetType().Name} ({objectId}): {ex.Message}");
                        component.InjectionState = DependencyInjectionState.Failed;
                    }
                }
                yield return new WaitForSeconds(0.1f * (attempt + 1));
            }
        }
    }
}