using _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Application;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallWorldResetServices()
        {
            RegisterIfMissing(() => new SceneFlowWorldResetDriver());
            RegisterIfMissing(() => new WorldResetService());
            RegisterIfMissing<IWorldResetCommands>(() => new WorldResetCommands());

            var gateService = ResolveSimulationGateServiceOrNull();
            RegisterIfMissing<IWorldResetRequestService>(() => new WorldResetRequestService(gateService));
        }
    }
}
