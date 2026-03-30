#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime
{
    /// <summary>
    /// Contrato para orquestrar um reset determinístico do WorldReset.
    /// Implementações devem garantir idempotência e determinismo por `reason/contextSignature`.
    /// Implementacao canonica: Lifecycle/World/Reset/Application/WorldResetService.
    /// </summary>
    public interface IWorldResetService
    {
        Task<WorldResetResult> TriggerResetAsync(string? contextSignature, string? reason);

        Task<WorldResetResult> TriggerResetAsync(WorldResetRequest request);
    }
}


