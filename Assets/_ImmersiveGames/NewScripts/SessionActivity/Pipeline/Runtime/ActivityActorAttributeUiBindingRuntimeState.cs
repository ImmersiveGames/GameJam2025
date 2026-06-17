using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.UI;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityActorAttributeUiBindingRuntimeState
    {
        private readonly List<ActorAttributeUiBindingHandle> _activeHandles = new();

        public int ActiveHandleCount => _activeHandles.Count;

        public void Store(ActorAttributeUiBindingHandle handle)
        {
            if (handle == null || handle.IsDisposed)
            {
                return;
            }

            _activeHandles.Add(handle);
        }

        public int ClearAll()
        {
            int releasedCount = _activeHandles.Count;

            for (int index = 0; index < _activeHandles.Count; index++)
            {
                _activeHandles[index]?.Dispose();
            }

            _activeHandles.Clear();
            return releasedCount;
        }
    }
}
