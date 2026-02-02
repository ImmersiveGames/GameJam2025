using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Lifecycle.World.Runtime
{
    /// <summary>
    /// Entry-point de produção para solicitar ResetWorld (hard reset) fora de QA.
    /// </summary>
    public interface IWorldResetRequestService
    {
        /// <summary>
        /// Solicita um ResetWorld no contexto atual (quando não há transição ativa).
        /// </summary>
        Task RequestResetAsync(string source);
    }
}
