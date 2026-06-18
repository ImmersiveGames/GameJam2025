using _ImmersiveGames.NewScripts.Foundation.Platform.SceneComposition;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallSceneCompositionServices()
        {
            RegisterIfMissing<ISceneCompositionExecutor>(
                () => new SceneCompositionExecutor(),
                "ISceneCompositionExecutor ja registrado no DI global.",
                "ISceneCompositionExecutor registrado (SceneCompositionExecutor).");
        }
    }
}
