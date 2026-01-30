// Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/NoopLevelManagerService.cs
#nullable enable

using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// No-op para ILevelManagerService (usado quando o gate está desabilitado).
    /// </summary>
    public sealed class NoopLevelManagerService : ILevelManagerService
    {
        public bool SelectLevel(string levelId, string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: SelectLevel ignorado levelId='{levelId}' reason='{reason}'.");
            return false;
        }

        public bool SelectInitialLevel(string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: SelectInitialLevel ignorado reason='{reason}'.");
            return false;
        }

        public bool SelectNextLevel(string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: SelectNextLevel ignorado reason='{reason}'.");
            return false;
        }

        public bool SelectPreviousLevel(string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: SelectPreviousLevel ignorado reason='{reason}'.");
            return false;
        }

        public Task ApplySelectedLevelAsync(string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: ApplySelectedLevelAsync ignorado reason='{reason}'.");
            return Task.CompletedTask;
        }

        public void ClearSelection(string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: ClearSelection ignorado reason='{reason}'.");
        }

        public void NotifyContentSwapCommitted(ContentSwapPlan plan, string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: NotifyContentSwapCommitted ignorado reason='{reason}'.");
        }

        public void DumpCurrent(string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: DumpCurrent ignorado reason='{reason}'.");
        }
    }
}
