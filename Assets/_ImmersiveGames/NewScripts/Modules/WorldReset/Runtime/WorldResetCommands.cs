using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Application;
namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetCommands : IWorldResetCommands
    {
        // OWNER boundary:
        // - V1: não publica nem controla o gate/correlacao do SceneFlow.
        // - V2: publica apenas telemetria/observabilidade de commands de reset.
        public async Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!macroRouteId.IsValid)
            {
                FailFastConfig($"ResetMacroAsync received invalid macroRouteId. reason='{reason ?? "<null>"}'.");
            }

            string normalizedReason = NormalizeReason(reason, "WorldReset/Macro");
            string normalizedMacroSignature = NormalizeSignature(macroSignature);

            PublishRequested(ResetKind.Macro, macroRouteId, normalizedReason, normalizedMacroSignature, LevelContextSignature.Empty);

            try
            {
                IWorldResetService resetService = ResolveMacroResetServiceOrFail();
                WorldResetResult resetResult = await resetService.TriggerResetAsync(normalizedMacroSignature, normalizedReason);
                bool success = resetResult == WorldResetResult.Completed;

                string notes = resetResult switch
                {
                    WorldResetResult.SkippedValidation => "ValidationFailed: ContextSignatureEmpty",
                    WorldResetResult.Failed => "ResetFailed",
                    _ => string.Empty
                };

                PublishCompleted(ResetKind.Macro, macroRouteId, normalizedReason, normalizedMacroSignature, LevelContextSignature.Empty, success, notes);
            }
            catch (Exception ex)
            {
                PublishCompleted(ResetKind.Macro, macroRouteId, normalizedReason, normalizedMacroSignature, LevelContextSignature.Empty, false, ex.GetType().Name);
                throw;
            }
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

                PublishRequested(ResetKind.Level, macroRouteId, normalizedReason, string.Empty, levelSignature);

                try
                {
                    PublishCompleted(ResetKind.Level, macroRouteId, normalizedReason, string.Empty, levelSignature, true, string.Empty);
                }
                catch (Exception ex)
                {
                    PublishCompleted(ResetKind.Level, macroRouteId, normalizedReason, string.Empty, levelSignature, false, ex.GetType().Name);
                    throw;
                }
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

            Debug.Assert(DependencyManager.Provider != null, "DependencyManager.Provider != null");
            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                FailFastConfig($"Missing required global service: {label}.");
            }

            return service;
        }

        private static void PublishRequested(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature)
        {
            DebugUtility.Log<WorldResetCommands>(
                $"[OBS][WorldReset] ResetRequestedV2 kind='{kind}' macroRouteId='{macroRouteId}' macroSignature='{macroSignature}' levelSignature='{levelSignature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldResetEvents>.Raise(new WorldResetEvents(
                kind,
                macroRouteId,
                reason,
                macroSignature,
                levelSignature));
        }

        private static void PublishCompleted(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            bool success,
            string notes)
        {
            DebugUtility.Log<WorldResetCommands>(
                $"[OBS][WorldReset] ResetCompletedV2 kind='{kind}' macroRouteId='{macroRouteId}' macroSignature='{macroSignature}' levelSignature='{levelSignature}' reason='{reason}' success={success} notes='{notes}'.",
                success ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            EventBus<ResetCompletedEvent>.Raise(new ResetCompletedEvent(
                kind,
                macroRouteId,
                reason,
                macroSignature,
                levelSignature,
                success,
                notes));
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
