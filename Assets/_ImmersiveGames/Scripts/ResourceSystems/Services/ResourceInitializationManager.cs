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
        // Pendentes: objectId -> componente
        public readonly Dictionary<string, IInjectableComponent> _pendingComponents = new();

        // controla tentativas por componente para evitar loops infinitos
        private readonly Dictionary<string, int> _attemptCounts = new();

        // limites configuráveis
        private const int MaxAttemptsPerComponent = 6;
        private const float BaseRetryDelay = 0.05f;

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            DebugUtility.Log<ResourceInitializationManager>(
                "✅ Initialization Manager Ready",
                DebugUtility.Colors.CrucialInfo);
        }

        /// <summary>
        /// Registra componente que precisa de injeção. Tenta injetar imediatamente e,
        /// se não for possível, agenda retries com backoff curto.
        /// </summary>
        public void RegisterForInjection(IInjectableComponent component)
        {
            if (component == null) return;

            string objectId = component.GetObjectId();
            if (string.IsNullOrEmpty(objectId))
            {
                DebugUtility.LogWarning<ResourceInitializationManager>($"RegisterForInjection called with empty object id for {component.GetType().Name}");
                return;
            }

            _pendingComponents[objectId] = component;
            _attemptCounts[objectId] = 0;

            // tenta injetar imediatamente (síncrono) - se falhar, inicia coroutine de retry
            TryInjectComponent(component);
        }

        private void TryInjectComponent(IInjectableComponent component)
        {
            string objectId = component.GetObjectId();
            if (string.IsNullOrEmpty(objectId)) return;

            try
            {
                DependencyManager.Provider.InjectDependencies(component, objectId);
                component.OnDependenciesInjected();
                _pendingComponents.Remove(objectId);
                _attemptCounts.Remove(objectId);

                DebugUtility.Log<ResourceInitializationManager>(
                    $"✅ Dependências injetadas para {component.GetType().Name} ({objectId})",
                    DebugUtility.Colors.Success);
            }
            catch (Exception)
            {
                // inicia rotina de retry se ainda não excedeu tentativas
                if (_attemptCounts.TryGetValue(objectId, out int attempts) && attempts < MaxAttemptsPerComponent)
                {
                    _attemptCounts[objectId] = attempts + 1;
                    StartCoroutine(InjectionRetryCoroutine(component, attempts));
                }
                else
                {
                    DebugUtility.LogError<ResourceInitializationManager>($"❌ Failed to inject dependencies for {component.GetType().Name} ({objectId}) after {MaxAttemptsPerComponent} attempts");
                    component.InjectionState = DependencyInjectionState.Failed;
                    _pendingComponents.Remove(objectId);
                    _attemptCounts.Remove(objectId);
                }
            }
        }

        private IEnumerator InjectionRetryCoroutine(IInjectableComponent component, int previousAttempts)
        {
            string objectId = component.GetObjectId();
            int attemptIndex = previousAttempts + 1;

            // backoff linear curto: base * attemptIndex
            float wait = BaseRetryDelay * (attemptIndex + 1);
            yield return new WaitForSeconds(wait);

            if (!_pendingComponents.ContainsKey(objectId) || component == null)
                yield break;

            try
            {
                DependencyManager.Provider.InjectDependencies(component, objectId);
                component.OnDependenciesInjected();
                _pendingComponents.Remove(objectId);
                _attemptCounts.Remove(objectId);

                DebugUtility.Log<ResourceInitializationManager>(
                    $"✅ (Retry) Dependências injetadas para {component.GetType().Name} ({objectId}) on attempt {attemptIndex}",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                if (_attemptCounts.TryGetValue(objectId, out int attempts) && attempts < MaxAttemptsPerComponent)
                {
                    _attemptCounts[objectId] = attempts + 1;
                    StartCoroutine(InjectionRetryCoroutine(component, attempts));
                }
                else
                {
                    DebugUtility.LogError<ResourceInitializationManager>($"❌ Failed to inject dependencies for {component.GetType().Name} ({objectId}): {ex.Message}");
                    component.InjectionState = DependencyInjectionState.Failed;
                    _pendingComponents.Remove(objectId);
                    _attemptCounts.Remove(objectId);
                }
            }
        }

        /// <summary>
        /// Método utilitário para forçar tentativa de injeção em todos pendentes.
        /// Útil durante debugging ou quando você sabe que uma dependência global acabou de ficar disponível.
        /// </summary>
        public void TryInjectAllPending()
        {
            var snapshot = new List<IInjectableComponent>(_pendingComponents.Values);
            foreach (var comp in snapshot)
            {
                TryInjectComponent(comp);
            }
        }
    }
}
