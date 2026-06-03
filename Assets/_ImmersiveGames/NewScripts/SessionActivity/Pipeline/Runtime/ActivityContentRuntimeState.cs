using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
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
            bool before = HasCurrentLoadedSet;
            _currentLoadedSet = loadedSet;
            LogStateChanged(
                "ActivityContentRuntimeStateLoadedSetStored",
                activityId,
                entrySequence,
                before,
                HasCurrentLoadedSet,
                source,
                reason);
        }

        public void ClearCurrentLoadedSet(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool before = HasCurrentLoadedSet;
            _currentLoadedSet = default;
            LogStateChanged(
                "ActivityContentRuntimeStateLoadedSetCleared",
                activityId,
                entrySequence,
                before,
                HasCurrentLoadedSet,
                source,
                reason);
        }

        private static void LogStateChanged(
            string eventName,
            string activityId,
            int entrySequence,
            bool hasLoadedSetBefore,
            bool hasLoadedSetAfter,
            string source,
            string reason)
        {
            DebugUtility.Log(
                typeof(ActivityContentRuntimeState),
                $"[OBS][ActivityContentRuntimeState] event='{Normalize(eventName)}' owner='ActivityContentRuntimeState' activityId='{Normalize(activityId)}' entrySequence='{entrySequence}' hasLoadedSetBefore='{ToLowerInvariant(hasLoadedSetBefore)}' hasLoadedSetAfter='{ToLowerInvariant(hasLoadedSetAfter)}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string ToLowerInvariant(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
