using _ImmersiveGames.NewScripts.Foundation.Platform.SceneComposition;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallSceneCompositionServices()
        {
            RegisterIfMissing<ISceneCompositionExecutor>(
                () => new SceneCompositionExecutor(),
                alreadyRegisteredMessage: "ISceneCompositionExecutor ja registrado no DI global.",
                registeredMessage: "ISceneCompositionExecutor registrado (SceneCompositionExecutor).");
        }
    }
}


