using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Application;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetCommands : IWorldResetCommands
    {
        // OWNER boundary:
        // - Commands de entrada pública para reset macro.
        // - Macro reset delega ao serviço canônico, que publica o contrato de lifecycle.
        public async Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!macroRouteId.IsValid)
            {
                FailFastConfig($"ResetMacroAsync received invalid macroRouteId. reason='{reason ?? "<null>"}'.");
            }

            string normalizedReason = NormalizeReason(reason, "WorldReset/Macro");
            string normalizedMacroSignature = NormalizeSignature(macroSignature);

            DebugUtility.Log<WorldResetCommands>(
                $"[OBS][WorldReset] ResetMacro command routeId='{macroRouteId}' macroSignature='{normalizedMacroSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            IWorldResetService resetService = ResolveMacroResetServiceOrFail();
            var request = new WorldResetRequest(
                kind: ResetKind.Macro,
                contextSignature: normalizedMacroSignature,
                reason: normalizedReason,
                targetScene: string.Empty,
                origin: WorldResetOrigin.Command,
                macroRouteId: macroRouteId,
                phaseSignature: PhaseContextSignature.Empty,
                sourceSignature: normalizedMacroSignature);

            await resetService.TriggerResetAsync(request);
        }

        private static IWorldResetService ResolveMacroResetServiceOrFail()
        {
            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<IWorldResetService>(out var byInterface) && byInterface != null)
            {
                return byInterface;
            }

            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<WorldResetService>(out var byConcrete) && byConcrete != null)
            {
                return byConcrete;
            }

            FailFastConfig("IWorldResetService/WorldResetService missing in global DI for ResetMacroAsync.");
            return null;
        }

        private static T ResolveGlobalOrFail<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                FailFastConfig($"DependencyManager.Provider is null while resolving {label}.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                FailFastConfig($"Missing required global service: {label}.");
            }

            return service;
        }

        private static string NormalizeReason(string reason, string fallback)
        {
            return string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
        }

        private static string NormalizeSignature(string signature)
        {
            return string.IsNullOrWhiteSpace(signature) ? string.Empty : signature.Trim();
        }

        private static void FailFastConfig(string detail)
        {
            HardFailFastH1.Trigger(typeof(WorldResetCommands), $"[FATAL][H1][WorldReset] {detail}");
        }
    }
}
