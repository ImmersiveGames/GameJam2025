using UnityEngine;
using _ImmersiveGames.NewProject.Infrastructure.DependencyInjection;
using _ImmersiveGames.NewProject.Infrastructure.Events;
using _ImmersiveGames.NewProject.Infrastructure.Identity;
using _ImmersiveGames.NewProject.Infrastructure.Logging;
using _ImmersiveGames.NewProject.Infrastructure.Simulation;

namespace _ImmersiveGames.NewProject.Infrastructure.Bootstrap
{
    /// <summary>
    /// Ponto único de entrada para registrar serviços globais do projeto novo.
    /// </summary>
    public static class GlobalBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            var services = new ServiceCollection();
            services
                .RegisterSingleton<ILogger>(_ => new UnityLogger("NewProject", LogLevel.Info))
                .RegisterSingleton<IEventBus>(provider => new TypedEventBus(provider.Resolve<ILogger>()))
                .RegisterSingleton<IUniqueIdFactory>(provider => new UniqueIdFactory(provider.Resolve<ILogger>()), ServiceScope.Global)
                .RegisterSingleton<ISimulationGate>(provider => new SimulationGate(provider.Resolve<ILogger>()), ServiceScope.Global);

            var provider = ServiceBuilder.Build(services);
            GlobalServiceLocator.Initialize(provider);

            var logger = provider.Resolve<ILogger>();
            logger.Log(LogLevel.Info, "Infra inicializada");
        }
    }
}
