using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Application;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Implementação de comandos explícitos de reset (macro x level).
    /// Mantém compatibilidade com reset legado e adiciona observabilidade V2.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetCommands : IWorldResetCommands
    {
        public async Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!macroRouteId.IsValid)
            {
                FailFastConfig($"ResetMacroAsync recebeu macroRouteId inválido. reason='{reason ?? "<null>"}'.");
                return;
            }

            string normalizedReason = NormalizeReason(reason, "WorldReset/Macro");
            string normalizedMacroSignature = NormalizeSignature(macroSignature);

            PublishRequested(
                kind: ResetKind.Macro,
                macroRouteId: macroRouteId,
                levelId: LevelId.None,
                contentId: string.Empty,
                reason: normalizedReason,
                macroSignature: normalizedMacroSignature,
                levelSignature: LevelContextSignature.Empty);

            try
            {
                IWorldResetService resetService = ResolveMacroResetServiceOrFail();
                await resetService.TriggerResetAsync(normalizedMacroSignature, normalizedReason);

                PublishCompleted(
                    kind: ResetKind.Macro,
                    macroRouteId: macroRouteId,
                    levelId: LevelId.None,
                    contentId: string.Empty,
                    reason: normalizedReason,
                    macroSignature: normalizedMacroSignature,
                    levelSignature: LevelContextSignature.Empty,
                    success: true,
                    notes: string.Empty);
            }
            catch (Exception ex)
            {
                PublishCompleted(
                    kind: ResetKind.Macro,
                    macroRouteId: macroRouteId,
                    levelId: LevelId.None,
                    contentId: string.Empty,
                    reason: normalizedReason,
                    macroSignature: normalizedMacroSignature,
                    levelSignature: LevelContextSignature.Empty,
                    success: false,
                    notes: ex.GetType().Name);
                throw;
            }
        }

        public async Task ResetLevelAsync(LevelId levelId, string reason, LevelContextSignature levelSignature, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!levelId.IsValid)
            {
                FailFastConfig($"ResetLevelAsync recebeu levelId inválido. reason='{reason ?? "<null>"}'.");
                return;
            }

            if (!levelSignature.IsValid)
            {
                FailFastConfig($"ResetLevelAsync recebeu levelSignature vazia. levelId='{levelId}', reason='{reason ?? "<null>"}'.");
                return;
            }

            string normalizedReason = NormalizeReason(reason, "WorldReset/Level");

            var restartContext = ResolveGlobalOrFail<IRestartContextService>("IRestartContextService");
            var contentSwap = ResolveGlobalOrFail<IContentSwapChangeService>("IContentSwapChangeService");

            if (!restartContext.TryGetCurrent(out GameplayStartSnapshot snapshot) || !snapshot.IsValid)
            {
                FailFastConfig($"ResetLevelAsync sem snapshot válido no RestartContext. levelId='{levelId}', reason='{normalizedReason}'.");
                return;
            }

            if (!snapshot.HasLevelId || snapshot.LevelId != levelId)
            {
                FailFastConfig($"ResetLevelAsync com levelId divergente do snapshot atual. expected='{snapshot.LevelId}', got='{levelId}', reason='{normalizedReason}'.");
                return;
            }

            string contentId = ResolveCurrentContentIdOrFail(snapshot);
            SceneRouteId macroRouteId = snapshot.RouteId;

            PublishRequested(
                kind: ResetKind.Level,
                macroRouteId: macroRouteId,
                levelId: levelId,
                contentId: contentId,
                reason: normalizedReason,
                macroSignature: string.Empty,
                levelSignature: levelSignature);

            try
            {
                await contentSwap.RequestContentSwapInPlaceAsync(contentId, normalizedReason);

                PublishCompleted(
                    kind: ResetKind.Level,
                    macroRouteId: macroRouteId,
                    levelId: levelId,
                    contentId: contentId,
                    reason: normalizedReason,
                    macroSignature: string.Empty,
                    levelSignature: levelSignature,
                    success: true,
                    notes: string.Empty);
            }
            catch (Exception ex)
            {
                PublishCompleted(
                    kind: ResetKind.Level,
                    macroRouteId: macroRouteId,
                    levelId: levelId,
                    contentId: contentId,
                    reason: normalizedReason,
                    macroSignature: string.Empty,
                    levelSignature: levelSignature,
                    success: false,
                    notes: ex.GetType().Name);
                throw;
            }
        }

        private static string ResolveCurrentContentIdOrFail(GameplayStartSnapshot snapshot)
        {
            if (snapshot.HasContentId)
            {
                return snapshot.ContentId;
            }

            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<IContentSwapContextService>(out var contextService) &&
                contextService != null &&
                contextService.Current.IsValid &&
                !string.IsNullOrWhiteSpace(contextService.Current.contentId))
            {
                return contextService.Current.contentId.Trim();
            }

            FailFastConfig($"ResetLevelAsync sem contentId atual resolvível. routeId='{snapshot.RouteId}', levelId='{snapshot.LevelId}'.");
            return string.Empty;
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

            FailFastConfig("IWorldResetService/WorldResetService indisponível no DI global para ResetMacroAsync.");
            return null;
        }

        private static T ResolveGlobalOrFail<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                FailFastConfig($"DependencyManager.Provider nulo ao resolver {label}.");
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                FailFastConfig($"Serviço global obrigatório ausente: {label}.");
                return null;
            }

            return service;
        }

        private static void PublishRequested(
            ResetKind kind,
            SceneRouteId macroRouteId,
            LevelId levelId,
            string contentId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature)
        {
            DebugUtility.Log<WorldResetCommands>(
                $"[OBS][WorldLifecycle] ResetRequested kind='{kind}' macroRouteId='{macroRouteId}' levelId='{levelId}' contentId='{contentId}' macroSignature='{macroSignature}' levelSignature='{levelSignature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldLifecycleResetRequestedV2Event>.Raise(new WorldLifecycleResetRequestedV2Event(
                kind,
                macroRouteId,
                levelId,
                contentId,
                reason,
                macroSignature,
                levelSignature));
        }

        private static void PublishCompleted(
            ResetKind kind,
            SceneRouteId macroRouteId,
            LevelId levelId,
            string contentId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            bool success,
            string notes)
        {
            DebugUtility.Log<WorldResetCommands>(
                $"[OBS][WorldLifecycle] ResetCompleted kind='{kind}' macroRouteId='{macroRouteId}' levelId='{levelId}' contentId='{contentId}' macroSignature='{macroSignature}' levelSignature='{levelSignature}' reason='{reason}' success={success} notes='{notes}'.",
                success ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            EventBus<WorldLifecycleResetCompletedV2Event>.Raise(new WorldLifecycleResetCompletedV2Event(
                kind,
                macroRouteId,
                levelId,
                contentId,
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
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError<WorldResetCommands>(message);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }
    }
}
