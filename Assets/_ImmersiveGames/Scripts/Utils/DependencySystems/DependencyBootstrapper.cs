using System;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.PlanetSystems.Defense;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
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

            if (!DependencyManager.HasInstance)
            {
                var _ = DependencyManager.Provider;
            }

            Instance.RegisterEssentialServices();
        }

        private void RegisterEssentialServices()
        {
            try
            {
                EnsureGlobal<IUniqueIdFactory>(() => new UniqueIdFactory());

                // Fade + pré-carregamento da FadeScene
                EnsureGlobal<IFadeService>(() =>new FadeService());

                EnsureGlobal<ISceneLoader>(() => new SceneLoaderCore());

                EnsureGlobal<ISceneTransitionPlanner>(() => new SimpleSceneTransitionPlanner());

                EnsureGlobal<ISceneTransitionService>(() =>
                {
                    var provider = DependencyManager.Provider;

                    provider.TryGetGlobal(out ISceneLoader sceneLoader);
                    provider.TryGetGlobal(out IFadeService fadeService);

                    if (sceneLoader == null)
                    {
                        DebugUtility.LogWarning<DependencyBootstrapper>(
                            "[SceneTransition] ISceneLoader não encontrado ao criar SceneTransitionService. " +
                            "Transições de cena não funcionarão.");
                        return null;
                    }

                    if (fadeService == null)
                    {
                        DebugUtility.LogWarning<DependencyBootstrapper>(
                            "[SceneTransition] IFadeService não encontrado ao criar SceneTransitionService. " +
                            "Transições funcionarão, mas sem fade.");
                    }

                    return new SceneTransitionService(sceneLoader, fadeService);
                });

                var initManager = ResourceInitializationManager.Instance;
                EnsureGlobal(() => initManager);

                var pipelineManager = CanvasPipelineManager.Instance;
                EnsureGlobal(() => pipelineManager);

                EnsureGlobal<IActorResourceOrchestrator>(() => new ActorResourceOrchestratorService());

                initManager.RegisterForInjection(pipelineManager);
                if (DependencyManager.Provider.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator))
                {
                    initManager.RegisterForInjection((IInjectableComponent)orchestrator);
                }

                RegisterEventBuses();

                EnsureGlobal<IStateDependentService>(() => new StateDependentService(GameManagerStateMachine.Instance));

                EnsureGlobal(() => new DefenseStateManager());
                EnsureGlobal<IPlanetDefensePoolRunner>(() => new RealPlanetDefensePoolRunner());
                EnsureGlobal<IPlanetDefenseWaveRunner>(() =>
                {
                    var runner = new RealPlanetDefenseWaveRunner();
                    DependencyManager.Provider.InjectDependencies(runner);
                    runner.OnDependenciesInjected();
                    return runner;
                });

                DebugUtility.Log<DependencyBootstrapper>(
                    "✅ Essential dependency services registered",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<DependencyBootstrapper>($"Exception during bootstrap: {ex}");
            }
        }

        private void EnsureGlobal<T>(Func<T> factory) where T : class
        {
            if (!DependencyManager.Provider.TryGetGlobal<T>(out var existing) || existing == null)
            {
                var implementation = factory();
                DependencyManager.Provider.RegisterGlobal(implementation);
                DebugUtility.Log<DependencyBootstrapper>($"Registered global service: {typeof(T).Name}");
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

                    var tryGetMethod = typeof(DependencyManager)
                        .GetMethod("TryGetGlobal", BindingFlags.Instance | BindingFlags.Public);
                    var genericTryGet = tryGetMethod?.MakeGenericMethod(busInterfaceType);

                    bool exists = false;
                    if (genericTryGet != null)
                    {
                        var parameters = new object[] { null };
                        var result = (bool)genericTryGet.Invoke(DependencyManager.Provider, parameters);
                        exists = result;
                    }

                    if (!exists)
                    {
                        object busInstance = Activator.CreateInstance(busImplType);
                        var registerMethod = typeof(DependencyManager)
                            .GetMethod("RegisterGlobal", BindingFlags.Instance | BindingFlags.Public);
                        var genericRegister = registerMethod?.MakeGenericMethod(busInterfaceType);
                        genericRegister?.Invoke(DependencyManager.Provider, new[] { busInstance });

                        DebugUtility.Log<DependencyBootstrapper>($"Registered EventBus for {eventType.Name}");
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
