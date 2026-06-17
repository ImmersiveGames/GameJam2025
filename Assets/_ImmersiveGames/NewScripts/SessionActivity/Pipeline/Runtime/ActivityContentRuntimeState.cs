using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityContentRuntimeState
    {
        private ActivityContentLoadedSet _currentLoadedSet;

        public ActivityContentLoadedSet CurrentLoadedSet => _currentLoadedSet;
        public bool HasCurrentLoadedSet => _currentLoadedSet.IsValid || _currentLoadedSet.HasScenes;

        public void StoreCurrentLoadedSet(
            ActivityContentLoadedSet loadedSet,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _currentLoadedSet = loadedSet;
        }

        public void ClearCurrentLoadedSet(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _currentLoadedSet = default;
        }
    }
}
