#nullable enable
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Gameplay.WorldLifecycle
{
    /// <summary>
    /// Contrato para orquestrar um reset determinístico do WorldLifecycle.
    /// Implementações devem garantir idempotência e determinismo por `reason/contextSignature`.
    /// </summary>
    public interface IResetWorldService
    {
        Task TriggerResetAsync(string? contextSignature, string? reason);
    }
}
