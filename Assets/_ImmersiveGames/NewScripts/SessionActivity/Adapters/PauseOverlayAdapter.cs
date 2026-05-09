using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class PauseOverlayAdapter : ISessionActivityPauseOverlayAdapter
    {
        public void Show(SessionActivityIdentity identity, string source, string reason)
        {
            DebugUtility.Log(typeof(PauseOverlayAdapter),
                $"[OBS][SessionActivityPipeline][PauseOverlay] action='Show' identity='{identity}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        public void Hide(SessionActivityIdentity identity, string source, string reason)
        {
            DebugUtility.Log(typeof(PauseOverlayAdapter),
                $"[OBS][SessionActivityPipeline][PauseOverlay] action='Hide' identity='{identity}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}

