using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services
{
    public class RuntimeAttributeBootstrapper : PersistentSingleton<RuntimeAttributeBootstrapper>
    {
        // Pendentes: objectId -> componente
        private readonly Dictionary<string, IInjectableComponent> _pendingComponents = new();

        // controla tentativas por componente para evitar loops infinitos
        private readonly Dictionary<string, int> _attemptCounts = new();

        // limites configur�veis
        private const int MaxAttemptsPerComponent = 6;
        private const float BaseRetryDelay = 0.05f;

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            DebugUtility.LogVerbose<RuntimeAttributeBootstrapper>(
                "? Initialization Manager Ready",
                DebugUtility.Colors.CrucialInfo);
        }

        /// <summary>
        /// Registra componente que precisa de inje��o. Tenta injetar imediatamente e,
        /// se n�o for poss�vel, agenda retries com backoff curto.
        /// </summary>
        public void RegisterForInjection(IInjectableComponent component)
        {
            if (component == null) return;

            string objectId = component.GetObjectId();
            if (string.IsNullOrEmpty(objectId))
            {
                DebugUtility.LogWarning<RuntimeAttributeBootstrapper>($"RegisterForInjection called with empty object id for {component.GetType().Name}");
                return;
            }

            _pendingComponents[objectId] = component;
            _attemptCounts[objectId] = 0;

            // tenta injetar imediatamente (s�ncrono) - se falhar, inicia coroutine de retry
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

                DebugUtility.LogVerbose<RuntimeAttributeBootstrapper>(
                    $"? Depend�ncias injetadas para {component.GetType().Name} ({objectId})",
                    DebugUtility.Colors.Success);
            }
            catch (Exception)
            {
                // inicia rotina de retry se ainda n�o excedeu tentativas
                if (_attemptCounts.TryGetValue(objectId, out int attempts) && attempts < MaxAttemptsPerComponent)
                {
                    _attemptCounts[objectId] = attempts + 1;
                    StartCoroutine(InjectionRetryCoroutine(component, attempts));
                }
                else
                {
                    DebugUtility.LogError<RuntimeAttributeBootstrapper>($"? Failed to inject dependencies for {component.GetType().Name} ({objectId}) after {MaxAttemptsPerComponent} attempts");
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

            if (!_pendingComponents.ContainsKey(objectId))
                yield break;

            try
            {
                DependencyManager.Provider.InjectDependencies(component, objectId);
                component.OnDependenciesInjected();
                _pendingComponents.Remove(objectId);
                _attemptCounts.Remove(objectId);

                DebugUtility.LogVerbose<RuntimeAttributeBootstrapper>(
                    $"? (Retry) Depend�ncias injetadas para {component.GetType().Name} ({objectId}) on attempt {attemptIndex}",
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
                    DebugUtility.LogError<RuntimeAttributeBootstrapper>($"? Failed to inject dependencies for {component.GetType().Name} ({objectId}): {ex.Message}");
                    component.InjectionState = DependencyInjectionState.Failed;
                    _pendingComponents.Remove(objectId);
                    _attemptCounts.Remove(objectId);
                }
            }
        }

        /// <summary>
        /// M�todo utilit�rio para for�ar tentativa de inje��o em todos os pendentes.
        /// �til durante debugging ou quando voc� sabe que uma depend�ncia global acabou de ficar dispon�vel.
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

