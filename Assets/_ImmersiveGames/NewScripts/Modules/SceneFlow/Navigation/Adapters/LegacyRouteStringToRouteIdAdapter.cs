using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters
{
    /// <summary>
    /// Adapter temporário: converte routeId (string) legado para SceneRouteId.
    /// </summary>
    public static class LegacyRouteStringToRouteIdAdapter
    {
        public static SceneRouteId Adapt(string legacyRouteId)
        {
            return new SceneRouteId(legacyRouteId);
        }
    }
}
