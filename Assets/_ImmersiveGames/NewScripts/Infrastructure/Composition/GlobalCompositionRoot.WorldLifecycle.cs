using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Application;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallWorldLifecycleServices()
        {
            RegisterIfMissing(() => new WorldLifecycleSceneFlowResetDriver());
            RegisterIfMissing(() => new WorldResetService());
            RegisterIfMissing<IWorldResetCommands>(() => new WorldResetCommands());

            var gateService = ResolveSimulationGateServiceOrNull();
            RegisterIfMissing<IWorldResetRequestService>(() => new WorldResetRequestService(gateService));
        }
    }
}
