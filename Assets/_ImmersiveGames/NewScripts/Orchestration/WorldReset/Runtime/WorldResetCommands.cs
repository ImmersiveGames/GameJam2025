using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Application;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetCommands : IWorldResetCommands
    {
        private readonly WorldResetLifecyclePublisher _lifecyclePublisher = new();

        // OWNER boundary:
        // - Commands de entrada pública para reset macro/level.
        // - Macro reset delega ao serviço canônico, que publica o contrato de lifecycle.
        // - Level reset publica diretamente o mesmo contrato canônico.
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
                levelSignature: LevelContextSignature.Empty,
                sourceSignature: normalizedMacroSignature);

            await resetService.TriggerResetAsync(request);
        }

        public Task ResetLevelAsync(PhaseResetContext resetContext, string reason, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!resetContext.IsValid)
                {
                    FailFastConfig($"ResetLevelAsync received invalid phase reset context. reason='{reason ?? "<null>"}'.");
                }

                if (resetContext.PhaseDefinitionRef == null)
                {
                    FailFastConfig($"ResetLevelAsync received null phaseDefinitionRef. routeId='{resetContext.MacroRouteId}', reason='{reason ?? "<null>"}'.");
                }

                if (!resetContext.LevelSignature.IsValid)
                {
                    FailFastConfig($"ResetLevelAsync received empty levelSignature. phaseRef='{resetContext.PhaseDefinitionRef.name}', reason='{reason ?? "<null>"}'.");
                }

                string normalizedReason = NormalizeReason(reason, "WorldReset/Level");
                IRestartContextService restartContext = ResolveGlobalOrFail<IRestartContextService>("IRestartContextService");
                if (!restartContext.TryGetCurrent(out GameplayStartSnapshot snapshot) || !snapshot.IsValid || !snapshot.HasPhaseDefinitionRef)
                {
                    FailFastConfig($"ResetLevelAsync without valid gameplay phase snapshot. phaseRef='{resetContext.PhaseDefinitionRef.name}', reason='{normalizedReason}'.");
                }

                if (!ReferenceEquals(snapshot.PhaseDefinitionRef, resetContext.PhaseDefinitionRef))
                {
                    FailFastConfig($"ResetLevelAsync phaseDefinitionRef mismatch. expected='{snapshot.PhaseDefinitionRef.name}', got='{resetContext.PhaseDefinitionRef.name}', reason='{normalizedReason}'.");
                }

                if (snapshot.MacroRouteId != resetContext.MacroRouteId)
                {
                    FailFastConfig($"ResetLevelAsync macroRouteId mismatch. expected='{snapshot.MacroRouteId}', got='{resetContext.MacroRouteId}', reason='{normalizedReason}'.");
                }

                DebugUtility.Log<WorldResetCommands>(
                    $"[OBS][WorldReset] ResetLevel phaseRef='{resetContext.PhaseDefinitionRef.name}' routeId='{resetContext.MacroRouteId}' levelSignature='{resetContext.LevelSignature}' resetSignature='{resetContext.ResetSignature}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                var request = new WorldResetRequest(
                    kind: ResetKind.Level,
                    contextSignature: resetContext.ResetSignature,
                    reason: normalizedReason,
                    targetScene: string.Empty,
                    origin: WorldResetOrigin.Command,
                    macroRouteId: resetContext.MacroRouteId,
                    levelSignature: resetContext.LevelSignature,
                    sourceSignature: resetContext.ResetSignature);

                _lifecyclePublisher.PublishStarted(request);
                _lifecyclePublisher.PublishCompleted(request, WorldResetOutcome.Completed, string.Empty);

                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
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
            HardFailFastH1.Trigger(typeof(WorldResetCommands), $"[FATAL][H1][LevelFlow] {detail}");
        }
    }
}
