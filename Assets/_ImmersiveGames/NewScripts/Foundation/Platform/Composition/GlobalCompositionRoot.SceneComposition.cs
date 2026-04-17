using ImmersiveGames.GameJam2025.Orchestration.SceneComposition;
namespace ImmersiveGames.GameJam2025.Infrastructure.Composition
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

