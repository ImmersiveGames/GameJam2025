using _ImmersiveGames.NewScripts.Modules.SceneFlow.Interop;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterSceneFlowInputModeBridge()
        {
            if (!ShouldRegisterInputModeRuntimeRail())
            {
                return;
            }

            RegisterIfMissing(
                () => new SceneFlowInputModeBridge(),
                "[InputMode] SceneFlowInputModeBridge ja registrado no DI global.",
                "[InputMode] SceneFlowInputModeBridge registrado no DI global.");
        }
    }
}
