using System;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure
{
    /// <summary>
    /// Entry point for the new isolated project. It wires global infrastructure
    /// without altering or refactoring the legacy systems.
    /// </summary>
    public static class GlobalBootstrap
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            InitializeLogging();
            EnsureDependencyProvider();
            InitializeEventBusInfrastructure();
            RegisterEssentialServices();

            DebugUtility.Log<GlobalBootstrap>(
                "✅ Global infrastructure initialized (no gameplay started).",
                DebugUtility.Colors.Success);
        }

        private static void InitializeLogging()
        {
            DebugUtility.SetDefaultDebugLevel(DebugLevel.Warning);
            DebugUtility.LogVerbose<GlobalBootstrap>("Logging configured for new project bootstrap.");
        }

        private static void EnsureDependencyProvider()
        {
            if (!DependencyManager.HasInstance)
            {
                _ = DependencyManager.Provider;
                DebugUtility.LogVerbose<GlobalBootstrap>("DependencyManager created for global scope.");
            }
        }

        private static void InitializeEventBusInfrastructure()
        {
            IReadOnlyList<Type> eventTypes = EventBusUtil.EventTypes;
            if (eventTypes == null || eventTypes.Count == 0)
            {
                DebugUtility.LogWarning<GlobalBootstrap>("EventBusUtil returned no event types.");
                return;
            }

            foreach (var eventType in eventTypes)
            {
                var busInterfaceType = typeof(IEventBus<>).MakeGenericType(eventType);
                var busImplType = typeof(InjectableEventBus<>).MakeGenericType(eventType);

                if (TryGetRegistered(busInterfaceType, out _))
                {
                    continue;
                }

                object busInstance = Activator.CreateInstance(busImplType);
                RegisterGlobal(busInterfaceType, busInstance);
                DebugUtility.LogVerbose<GlobalBootstrap>($"Registered injectable EventBus for {eventType.Name}.");
            }
        }

        private static void RegisterEssentialServices()
        {
            RegisterIfMissing<IUniqueIdFactory>(() => new UniqueIdFactory());
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());
        }

        private static void RegisterIfMissing<T>(Func<T> factory) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var service) && service != null)
            {
                return;
            }

            var instance = factory();
            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose<GlobalBootstrap>($"Registered global service: {typeof(T).Name}.");
        }

        private static bool TryGetRegistered(Type type, out object instance)
        {
            var tryGetMethod = typeof(DependencyManager)
                .GetMethod("TryGetGlobal", BindingFlags.Instance | BindingFlags.Public);
            var genericTryGet = tryGetMethod?.MakeGenericMethod(type);
            var parameters = new object[] { null };
            bool exists = genericTryGet != null &&
                          (bool)genericTryGet.Invoke(DependencyManager.Provider, parameters);
            instance = parameters[0];
            return exists && instance != null;
        }

        private static void RegisterGlobal(Type type, object instance)
        {
            var registerMethod = typeof(DependencyManager)
                .GetMethod("RegisterGlobal", BindingFlags.Instance | BindingFlags.Public);
            var genericRegister = registerMethod?.MakeGenericMethod(type);
            genericRegister?.Invoke(DependencyManager.Provider, new[] { instance, (object)false });
        }
    }
}
