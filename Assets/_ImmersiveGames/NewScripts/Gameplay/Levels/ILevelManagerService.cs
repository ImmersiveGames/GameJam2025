#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Serviço de alto nível para seleção/aplicação de níveis e sincronização com ContentSwap.
    /// </summary>
    public interface ILevelManagerService
    {
        bool SelectLevel(string levelId, string reason);
        bool SelectInitialLevel(string reason);
        bool SelectNextLevel(string reason);
        bool SelectPreviousLevel(string reason);
        Task ApplySelectedLevelAsync(string reason);
        void ClearSelection(string reason);
        void NotifyContentSwapCommitted(ContentSwapPlan plan, string reason);
        void DumpCurrent(string reason);
    }
}
