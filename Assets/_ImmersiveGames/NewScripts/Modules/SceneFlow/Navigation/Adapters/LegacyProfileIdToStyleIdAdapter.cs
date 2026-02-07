using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters
{
    /// <summary>
    /// Adapter temporário: converte SceneFlowProfileId legado para TransitionStyleId.
    /// </summary>
    public static class LegacyProfileIdToStyleIdAdapter
    {
        public static TransitionStyleId Adapt(SceneFlowProfileId legacyProfileId)
        {
            return new TransitionStyleId(legacyProfileId.Value);
        }
    }
}
