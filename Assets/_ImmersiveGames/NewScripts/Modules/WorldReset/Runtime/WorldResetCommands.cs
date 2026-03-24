using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Application;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetCommands : IWorldResetCommands
    {
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
                contextSignature: normalizedMacroSignature,
                reason: normalizedReason,
                targetScene: string.Empty,
                origin: WorldResetOrigin.Command,
                macroRouteId: macroRouteId,
                levelSignature: LevelContextSignature.Empty,
                sourceSignature: normalizedMacroSignature);

            await resetService.TriggerResetAsync(request);
        }

        public Task ResetLevelAsync(LevelDefinitionAsset levelRef, string reason, LevelContextSignature levelSignature, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (levelRef == null)
                {
                    FailFastConfig($"ResetLevelAsync received null levelRef. reason='{reason ?? "<null>"}'.");
                }

                if (!levelSignature.IsValid)
                {
                    FailFastConfig($"ResetLevelAsync received empty levelSignature. levelRef='{levelRef.name}', reason='{reason ?? "<null>"}'.");
                }

                string normalizedReason = NormalizeReason(reason, "WorldReset/Level");
                var restartContext = ResolveGlobalOrFail<IRestartContextService>("IRestartContextService");
                if (!restartContext.TryGetCurrent(out GameplayStartSnapshot snapshot) || !snapshot.IsValid)
                {
                    FailFastConfig($"ResetLevelAsync without valid gameplay snapshot. levelRef='{levelRef.name}', reason='{normalizedReason}'.");
                }

                if (!snapshot.HasLevelRef || !ReferenceEquals(snapshot.LevelRef, levelRef))
                {
                    FailFastConfig($"ResetLevelAsync levelRef mismatch. expected='{(snapshot.HasLevelRef ? snapshot.LevelRef.name : "<none>")}', got='{levelRef.name}', reason='{normalizedReason}'.");
                }

                SceneRouteId macroRouteId = snapshot.MacroRouteId;

                PublishStarted(
                    ResetKind.Level,
                    macroRouteId,
                    normalizedReason,
                    string.Empty,
                    levelSignature,
                    WorldResetOrigin.Command,
                    string.Empty);

                PublishCompleted(
                    ResetKind.Level,
                    macroRouteId,
                    normalizedReason,
                    string.Empty,
                    levelSignature,
                    WorldResetOutcome.Completed,
                    string.Empty,
                    WorldResetOrigin.Command,
                    string.Empty);

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

        private static void PublishStarted(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string contextSignature,
            LevelContextSignature levelSignature,
            WorldResetOrigin origin,
            string sourceSignature)
        {
            DebugUtility.Log<WorldResetCommands>(
                $"[OBS][WorldReset] ResetStarted kind='{kind}' routeId='{macroRouteId}' contextSignature='{contextSignature}' levelSignature='{levelSignature}' reason='{reason}' origin='{origin}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldResetStartedEvent>.Raise(new WorldResetStartedEvent(
                kind,
                macroRouteId,
                reason,
                contextSignature,
                levelSignature,
                origin,
                sourceSignature));
        }

        private static void PublishCompleted(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string contextSignature,
            LevelContextSignature levelSignature,
            WorldResetOutcome outcome,
            string detail,
            WorldResetOrigin origin,
            string sourceSignature)
        {
            DebugUtility.Log<WorldResetCommands>(
                $"[OBS][WorldReset] ResetCompleted kind='{kind}' routeId='{macroRouteId}' contextSignature='{contextSignature}' levelSignature='{levelSignature}' reason='{reason}' outcome='{outcome}' detail='{detail}' origin='{origin}'.",
                outcome == WorldResetOutcome.Completed || outcome == WorldResetOutcome.SkippedByPolicy || outcome == WorldResetOutcome.SkippedValidation
                    ? DebugUtility.Colors.Success
                    : DebugUtility.Colors.Warning);

            EventBus<WorldResetCompletedEvent>.Raise(new WorldResetCompletedEvent(
                kind,
                macroRouteId,
                reason,
                contextSignature,
                levelSignature,
                outcome,
                detail,
                origin,
                sourceSignature));
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
