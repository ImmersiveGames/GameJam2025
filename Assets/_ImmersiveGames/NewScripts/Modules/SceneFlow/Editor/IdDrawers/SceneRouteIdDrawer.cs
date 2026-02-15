using _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdDrawers
{
    [CustomPropertyDrawer(typeof(SceneRouteId))]
    public sealed class SceneRouteIdDrawer : SceneFlowTypedIdDrawerBase
    {
        private static readonly SceneRouteIdSourceProvider Provider = new();

        protected override SceneFlowIdSourceResult CollectSource()
        {
            return Provider.Collect();
        }
    }
}
