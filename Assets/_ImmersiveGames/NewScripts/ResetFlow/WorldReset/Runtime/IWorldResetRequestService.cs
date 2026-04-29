#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    /// <summary>
    /// Entry-point de produção para solicitar ResetWorld (hard reset) fora de QA.
    /// </summary>
    public interface IWorldResetRequestService
    {
        /// <summary>
        /// Solicita um ResetWorld já materializado pelo caller canônico.
        /// </summary>
        Task RequestResetAsync(WorldResetRequest request);
    }
}

