#nullable enable
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime
{
    /// <summary>
    /// Resolver padrao de politica da IntroStageController (preparado para producao).
    /// Decide apenas pela semantica canonica de rota.
    /// </summary>
    public sealed class DefaultIntroStagePolicyResolver : IIntroStagePolicyResolver
    {
        public IntroStagePolicy Resolve(SceneRouteKind routeKind, string reason)
        {
            return routeKind == SceneRouteKind.Gameplay
                ? IntroStagePolicy.Manual
                : IntroStagePolicy.Disabled;
        }
    }
}
