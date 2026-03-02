using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Serviço de domínio para troca local de level dentro do mesmo macroRoute.
    /// Não dispara transição macro do SceneFlow.
    /// </summary>
    public interface ILevelSwapLocalService
    {
        Task SwapLocalAsync(LevelId levelId, string reason = null, CancellationToken ct = default);
    }
}
