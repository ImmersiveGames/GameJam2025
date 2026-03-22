using _ImmersiveGames.NewScripts.Infrastructure.SceneComposition;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallSceneCompositionServices()
        {
            RegisterIfMissing<ISceneCompositionExecutor>(
                () => new LevelSceneCompositionExecutor(),
                alreadyRegisteredMessage: "[OBS][SceneComposition] ISceneCompositionExecutor ja registrado no DI global.",
                registeredMessage: "[OBS][SceneComposition] ISceneCompositionExecutor registrado (LevelSceneCompositionExecutor).");
        }
    }
}
