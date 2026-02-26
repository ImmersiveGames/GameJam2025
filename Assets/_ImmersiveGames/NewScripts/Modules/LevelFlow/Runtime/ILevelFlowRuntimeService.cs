using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Serviço público/canônico para iniciar gameplay a partir de um LevelId lógico.
    /// </summary>
    public interface ILevelFlowRuntimeService
    {
        Task StartGameplayAsync(string levelId, string reason = null, CancellationToken ct = default);
        /// <summary>
        /// Reinicia o gameplay usando o snapshot canônico, incrementa selectionVersion,
        /// publica LevelSelectedEvent e depois dispara a navegação para a rota resolvida do level.
        /// </summary>
        Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default);
        Task SwapLevelLocalAsync(LevelId levelId, string reason = null, CancellationToken ct = default);
    }
}
