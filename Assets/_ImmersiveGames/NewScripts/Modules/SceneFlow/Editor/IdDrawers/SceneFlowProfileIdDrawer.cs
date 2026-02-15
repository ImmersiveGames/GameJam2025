using _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdDrawers
{
    [CustomPropertyDrawer(typeof(SceneFlowProfileId))]
    public sealed class SceneFlowProfileIdDrawer : SceneFlowTypedIdDrawerBase
    {
        private static readonly SceneFlowProfileIdSourceProvider Provider = new();

        protected override SceneFlowIdSourceResult CollectSource()
        {
            return Provider.Collect();
        }
    }
}
