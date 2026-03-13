#nullable enable
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime
{
    /// <summary>
    /// Resolver padrao de politica da IntroStageController.
    /// A Intro continua globalmente orquestrada, mas o level atual pode expor se ela existe ou nao.
    /// </summary>
    public sealed class DefaultIntroStagePolicyResolver : IIntroStagePolicyResolver
    {
        public IntroStagePolicy Resolve(SceneRouteKind routeKind, string reason)
        {
            if (routeKind != SceneRouteKind.Gameplay)
            {
                return IntroStagePolicy.Disabled;
            }

            if (DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var stagePresentationService) &&
                stagePresentationService != null &&
                stagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract))
            {
                return contract.HasIntroStage
                    ? IntroStagePolicy.Manual
                    : IntroStagePolicy.Disabled;
            }

            return IntroStagePolicy.Manual;
        }
    }
}
