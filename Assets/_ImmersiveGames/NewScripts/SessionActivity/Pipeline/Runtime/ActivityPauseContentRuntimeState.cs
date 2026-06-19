using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    public sealed class ActivityPauseContentRuntimeState
    {
        public ActivityPauseContentBindingResult CurrentBinding { get; private set; }
        public bool HasCurrentBinding => CurrentBinding.IsValid && CurrentBinding.HasBoundContent;

        public void StoreCurrentBinding(ActivityPauseContentBindingResult binding, string source, string reason)
        {
            if (!binding.IsValid)
            {
                return;
            }

            CurrentBinding = binding;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public void ClearCurrentBinding(string source, string reason)
        {
            CurrentBinding = default;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string Source { get; private set; }
        public string Reason { get; private set; }
    }
}
