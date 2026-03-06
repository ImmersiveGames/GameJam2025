using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public interface ILevelMacroRouteCatalog
    {
        bool TryResolveMacroRouteId(LevelId levelId, out SceneRouteId macroRouteId);
        bool TryGetLevelsForMacroRoute(SceneRouteId macroRouteId, out IReadOnlyList<LevelId> levelIds);
        bool TryGetNextLevelInMacro(LevelId currentLevelId, out LevelId nextLevelId, bool wrapToFirst = true);
    }
}
