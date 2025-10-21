﻿using System;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    
    public class DependencyBootstrapper : PersistentSingleton<DependencyBootstrapper>
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            DebugUtility.SetDefaultDebugLevel(DebugLevel.Warning);

            // Garantir criação do DependencyManager
            if (!DependencyManager.HasInstance)
            {
                var _ = DependencyManager.Instance; // força criação
            }

            Instance.RegisterEssentialServices();
        }

        private void RegisterEssentialServices()
        {
            try
            {
                // Serviços "puros" que não dependem de outros
                EnsureGlobal<IUniqueIdFactory>(() => new UniqueIdFactory());

                // ResourceInitializationManager - singleton próprio
                var initManager = ResourceInitializationManager.Instance;
                EnsureGlobal(() => initManager);

                // CanvasPipelineManager - singleton próprio
                var pipelineManager = CanvasPipelineManager.Instance;
                EnsureGlobal(() => pipelineManager);

                // Orchestrator - depende do pipeline (mas não forçamos injeção aqui).
                // Registramos globalmente se ainda não existir.
                EnsureGlobal<IActorResourceOrchestrator>(() => new ActorResourceOrchestratorService());

                // Registrar injeções para serviços globais no initManager (garante que receberão DI se precisarem)
                initManager.RegisterForInjection(pipelineManager);
                if (DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator))
                {
                    initManager.RegisterForInjection((IInjectableComponent)orchestrator);
                }

                // Registrar EventBuses (dinâmicos) - idempotente
                RegisterEventBuses();

                // Registrar StateDependentService se não houver
                EnsureGlobal<IStateDependentService>(() => new StateDependentService(GameManagerStateMachine.Instance));

                DebugUtility.Log<DependencyBootstrapper>(
                    "✅ Essential dependency services registered",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<DependencyBootstrapper>($"Exception during bootstrap: {ex}");
            }
        }

        // Garante registro global idempotente com fábrica
        private void EnsureGlobal<T>(Func<T> factory) where T : class
        {
            if (!DependencyManager.Instance.TryGetGlobal<T>(out var existing) || existing == null)
            {
                var implementation = factory();
                DependencyManager.Instance.RegisterGlobal<T>(implementation);
                DebugUtility.LogVerbose<DependencyBootstrapper>($"Registered global service: {typeof(T).Name}");
            }
            else
            {
                DebugUtility.LogVerbose<DependencyBootstrapper>($"Global service already present: {typeof(T).Name}");
            }
        }
        

        private static void RegisterEventBuses()
        {
            try
            {
                IReadOnlyList<Type> eventTypes = EventBusUtil.EventTypes;
                if (eventTypes == null) return;

                foreach (var eventType in eventTypes)
                {
                    var busInterfaceType = typeof(IEventBus<>).MakeGenericType(eventType);
                    var busImplType = typeof(InjectableEventBus<>).MakeGenericType(eventType);

                    // verifica se já existe um registro para esse tipo
                    var registerMethod = typeof(DependencyManager).GetMethod("TryGetGlobal", BindingFlags.Instance | BindingFlags.Public);
                    var genericTryGet = registerMethod?.MakeGenericMethod(busInterfaceType);

                    bool exists = false;
                    if (genericTryGet != null)
                    {
                        // invoca TryGetGlobal<T>(out T existing)
                        var parameters = new object[] { null };
                        var result = (bool)genericTryGet.Invoke(DependencyManager.Instance, parameters);
                        exists = result;
                    }

                    if (!exists)
                    {
                        object busInstance = Activator.CreateInstance(busImplType);
                        var registerMethodGeneric = typeof(DependencyManager).GetMethod("RegisterGlobal", BindingFlags.Instance | BindingFlags.Public);
                        var genericRegister = registerMethodGeneric?.MakeGenericMethod(busInterfaceType);
                        genericRegister?.Invoke(DependencyManager.Instance, new[] { busInstance });
                        DebugUtility.LogVerbose<DependencyBootstrapper>($"Registered EventBus for {eventType.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<DependencyBootstrapper>($"Failed to register injectable EventBuses: {ex}");
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _initialized = false;
#endif
    }
}
