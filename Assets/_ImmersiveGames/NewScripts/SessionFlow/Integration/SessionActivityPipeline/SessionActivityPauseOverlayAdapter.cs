using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SessionActivityPipeline
{
    public sealed class SessionActivityPauseOverlayAdapter : ISessionActivityPauseOverlayAdapter
    {
        public void Show(SessionActivityIdentity identity, string source, string reason)
        {
            DebugUtility.Log(typeof(SessionActivityPauseOverlayAdapter),
                $"[OBS][SessionActivityPipeline][PauseOverlay] action='Show' identity='{identity}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        public void Hide(SessionActivityIdentity identity, string source, string reason)
        {
            DebugUtility.Log(typeof(SessionActivityPauseOverlayAdapter),
                $"[OBS][SessionActivityPipeline][PauseOverlay] action='Hide' identity='{identity}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}
