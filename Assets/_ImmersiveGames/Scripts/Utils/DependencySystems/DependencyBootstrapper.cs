using System;
using System.Reflection;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.DamageSystem.Services;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
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
            // EXISTENTES (mantenha os que você já tinha)
            DependencyManager.Instance.RegisterGlobal<IUniqueIdFactory>(new UniqueIdFactory());
            DependencyManager.Instance.RegisterGlobal<IStateDependentService>(new StateDependentService(GameManagerStateMachine.Instance));
            DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(new ActorResourceOrchestratorService());
            
            RegisterEventBuses();

            var effectService = RegisterEffectAndDamageServices();

            DependencyManager.Instance.TryGet<IActorResourceOrchestrator>(out var orchestrator);
            var damageService = new DamageService(effectService, orchestrator);
            DependencyManager.Instance.RegisterGlobal(damageService);
            
            DebugUtility.LogVerbose<DependencyBootstrapper>("Serviços essenciais registrados.");
        }
        private static EffectService RegisterEffectAndDamageServices()
        {
            // -------------------
            // New: EffectService & DamageService
            // -------------------
            var effectService = new EffectService(PoolManager.Instance, LifetimeManager.Instance);
            DependencyManager.Instance.RegisterGlobal(effectService);

            // Resolve IEventBus<DamageDealtEvent> from a dependency manager (must exist after bus registration above)
            try
            {
                DependencyManager.Instance.TryGet(out IEventBus<DamageDealtEvent> _);
            }
            catch
            {
                // ignored
            }
            return effectService;
        }
        private static void RegisterEventBuses()
        {
            // -------------------
            // NEW: register injectable buses for all event types (uses EventBusUtil.EventTypes)
            // -------------------
            try
            {
                var eventTypes = EventBusUtil.EventTypes;
                if (eventTypes != null)
                {
                    foreach (var eventType in eventTypes)
                    {
                        var busInterfaceType = typeof(IEventBus<>).MakeGenericType(eventType);
                        var busImplType = typeof(InjectableEventBus<>).MakeGenericType(eventType);

                        var busInstance = Activator.CreateInstance(busImplType);
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