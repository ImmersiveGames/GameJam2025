// Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/NoopLevelManagerService.cs
#nullable enable

using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// No-op para ILevelManagerService (usado quando o gate está desabilitado).
    /// </summary>
    public sealed class NoopLevelManagerService : ILevelManagerService
    {
        public int CurrentLevelIndex => -1;

        public ValueTask GoToLevelAsync(int levelIndex, string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: GoToLevelAsync ignorado levelIndex='{levelIndex}' reason='{reason}'.");
            return default;
        }

        public ValueTask RestartLevelAsync(string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: RestartLevelAsync ignorado reason='{reason}'.");
            return default;
        }

        public ValueTask AdvanceToNextLevelAsync(string reason)
        {
            DebugUtility.Log(typeof(NoopLevelManagerService),
                $"[LevelManager] Gate desabilitado: AdvanceToNextLevelAsync ignorado reason='{reason}'.");
            return default;
        }
    }
}
