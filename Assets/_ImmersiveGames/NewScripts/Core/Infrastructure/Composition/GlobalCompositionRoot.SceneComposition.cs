using _ImmersiveGames.NewScripts.Core.Infrastructure.SceneComposition;
namespace _ImmersiveGames.NewScripts.Core.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallSceneCompositionServices()
        {
            RegisterIfMissing<ISceneCompositionExecutor>(
                () => new SceneCompositionExecutor(),
                alreadyRegisteredMessage: "[OBS][SceneComposition] ISceneCompositionExecutor ja registrado no DI global.",
                registeredMessage: "[OBS][SceneComposition] ISceneCompositionExecutor registrado (SceneCompositionExecutor).");
        }
    }
}
