#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Domain;

namespace _ImmersiveGames.NewScripts.Runtime.World
{
    /// <summary>
    /// Contrato para orquestrar um reset determinístico do WorldLifecycle.
    /// Implementações devem garantir idempotência e determinismo por `reason/contextSignature`.
    /// Implementacao canonica: Lifecycle/World/Reset/Application/WorldResetService.
    /// </summary>
    public interface IResetWorldService
    {
        Task TriggerResetAsync(string? contextSignature, string? reason);

        Task TriggerResetAsync(WorldResetRequest request);
    }
}


