using _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdDrawers
{
    [CustomPropertyDrawer(typeof(TransitionStyleId))]
    public sealed class TransitionStyleIdDrawer : SceneFlowTypedIdDrawerBase
    {
        private static readonly TransitionStyleIdSourceProvider Provider = new();

        protected override SceneFlowIdSourceResult CollectSource()
        {
            return Provider.Collect();
        }
    }
}
