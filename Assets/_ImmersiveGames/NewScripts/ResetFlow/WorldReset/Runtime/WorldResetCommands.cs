using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetCommands : IWorldResetCommands
    {
        private readonly IWorldResetService _resetService;

        public WorldResetCommands(IWorldResetService resetService)
        {
            _resetService = resetService ?? throw new System.ArgumentNullException(nameof(resetService));
        }

        public async Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!macroRouteId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(WorldResetCommands),
                    $"[FATAL][H1][WorldReset] ResetMacroAsync received invalid macroRouteId. reason='{reason ?? "<null>"}'.");
            }

            string normalizedReason = NormalizeReason(reason, "WorldReset/Macro");
            string normalizedMacroSignature = NormalizeSignature(macroSignature);

            DebugUtility.Log<WorldResetCommands>(
                $"[OBS][WorldReset] ResetMacro command routeId='{macroRouteId}' macroSignature='{normalizedMacroSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            var request = new WorldResetRequest(
                kind: ResetKind.Macro,
                contextSignature: normalizedMacroSignature,
                reason: normalizedReason,
                targetScene: string.Empty,
                origin: WorldResetOrigin.Command,
                macroRouteId: macroRouteId,
                phaseSignature: PhaseContextSignature.Empty,
                sourceSignature: normalizedMacroSignature);

            await _resetService.TriggerResetAsync(request);
        }

        private static string NormalizeReason(string reason, string fallback)
        {
            return string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
        }

        private static string NormalizeSignature(string signature)
        {
            return string.IsNullOrWhiteSpace(signature) ? string.Empty : signature.Trim();
        }
    }
}

