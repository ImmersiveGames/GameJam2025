using System;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    [DebugLevel(DebugLevel.Logs)]
    public class DependencyBootstrapper : PersistentSingleton<DependencyBootstrapper>
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            DebugUtility.SetDefaultDebugLevel(DebugLevel.Verbose);
            
            // Garantir que DependencyManager existe
            if (!DependencyManager.HasInstance)
            {
                DependencyManager.Instance.ToString(); // Força criação
            }

            // Registrar serviços ESSENCIAIS (apenas os que precisam existir antes de tudo)
            Instance.RegisterEssentialServices();
        }

        private void RegisterEssentialServices()
        {

            RegisterBindCreations();
            DebugUtility.LogVerbose<DependencyBootstrapper>("Orchestrator Service Initialized");
            // EXISTENTES (mantenha os que você já tinha)
            DependencyManager.Instance.RegisterGlobal<IStateDependentService>(new StateDependentService(GameManagerStateMachine.Instance));
            
            RegisterEventBuses();
            
            DebugUtility.LogVerbose<DependencyBootstrapper>("Serviços essenciais registrados.");
        }
        private void RegisterBindCreations()
        {
            // PRIMEIRO: Serviços que não dependem de outros
            DependencyManager.Instance.RegisterGlobal<IUniqueIdFactory>(new UniqueIdFactory());
            // Registra o factory de slot de recursos
            DependencyManager.Instance.RegisterGlobal<IResourceSlotStrategyFactory>(new ResourceSlotStrategyFactory());

    
            // SEGUNDO: Gerenciadores
            var initManager = ResourceInitializationManager.Instance;
            DependencyManager.Instance.RegisterGlobal(initManager);

            var pipelineManager = CanvasPipelineManager.Instance;
            DependencyManager.Instance.RegisterGlobal(pipelineManager);

            // TERCEIRO: Orchestrator (depende do PipelineManager)
            var orchestrator = new ActorResourceOrchestratorService();
            DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(orchestrator);

            // QUARTO: Registrar para injeção dos serviços globais
            initManager.RegisterForInjection(pipelineManager);
            initManager.RegisterForInjection(orchestrator);
        }
        private static void RegisterEventBuses()
        {
            // -------------------
            // NEW: register injectable buses for all event types (uses EventBusUtil.EventTypes)
            // -------------------
            try
            {
                IReadOnlyList<Type> eventTypes = EventBusUtil.EventTypes;
                if (eventTypes != null)
                {
                    foreach (var eventType in eventTypes)
                    {
                        var busInterfaceType = typeof(IEventBus<>).MakeGenericType(eventType);
                        var busImplType = typeof(InjectableEventBus<>).MakeGenericType(eventType);

                        object busInstance = Activator.CreateInstance(busImplType);
                        var registerMethod = typeof(DependencyManager).GetMethod("RegisterGlobal", BindingFlags.Instance | BindingFlags.Public);
                        var genericRegister = registerMethod?.MakeGenericMethod(busInterfaceType);
                        // register the bus as global
                        genericRegister?.Invoke(DependencyManager.Instance, new[] { busInstance });
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